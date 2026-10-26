using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.News;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// Default <see cref="INewsDiscussionService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data access rules of the current user.</param>
/// <param name="events">Notifies open article pages.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class NewsDiscussionService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAccessScope accessScope,
    INewsDiscussionEvents events,
    IAuditLogger audit,
    TimeProvider timeProvider) : INewsDiscussionService
{
    /// <inheritdoc />
    public async Task<Result<NewsDiscussion>> GetAsync(Guid articleId, int limit, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(articleId, cancellationToken);
        if (access is null)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        var me = currentUser.Id;
        limit = Math.Clamp(limit, 1, 500);

        // Deleted top-level comments stay only as placeholders of their replies.
        var topLevel = await Rows(db.NewsComments
                .Where(c => c.ArticleId == articleId && c.ParentId == null)
                .Where(c => !c.IsDeleted || db.NewsComments.Any(r => r.ParentId == c.Id && !r.IsDeleted))
                .OrderByDescending(static c => c.CreatedAt)
                .Take(limit + 1))
            .ToListAsync(cancellationToken);
        var hasMore = topLevel.Count > limit;
        if (hasMore)
        {
            topLevel.RemoveAt(limit);
        }

        var parentIds = topLevel.ConvertAll(static c => c.Id);
        var replies = await Rows(db.NewsComments
                .Where(c => c.ParentId != null && parentIds.Contains(c.ParentId.Value) && !c.IsDeleted)
                .OrderBy(static c => c.CreatedAt))
            .ToListAsync(cancellationToken);

        var commentIds = parentIds.Concat(replies.Select(static r => r.Id)).ToList();
        var counts = await db.NewsReactions
            .Where(r => r.ArticleId == articleId && (r.CommentId == null || commentIds.Contains(r.CommentId.Value)))
            .GroupBy(static r => new { r.CommentId, r.Kind })
            .Select(static g => new { g.Key.CommentId, g.Key.Kind, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var mine = me is { } user
            ? await db.NewsReactions
                .Where(r => r.ArticleId == articleId && r.UserId == user)
                .Select(static r => new { r.CommentId, r.Kind })
                .ToDictionaryAsync(static r => r.CommentId ?? Guid.Empty, static r => (ReactionKind?)r.Kind, cancellationToken)
            : [];

        ReactionSummary SummaryOf(Guid? target) => ReactionSummary.From(
            counts.Where(c => c.CommentId == target).Select(static c => new ReactionCount(c.Kind, c.Count)),
            mine.GetValueOrDefault(target ?? Guid.Empty));

        NewsCommentDto ToDto(CommentRow row, IReadOnlyList<NewsCommentDto> children) => new(
            row.Id,
            row.ParentId,
            new UserRef(row.AuthorId, FullName(row), row.PhotoId),
            row.IsDeleted ? string.Empty : row.Body,
            row.CreatedAt,
            row.EditedAt,
            row.IsDeleted,
            SummaryOf(row.Id),
            !row.IsDeleted && row.AuthorId == me,
            !row.IsDeleted && (row.AuthorId == me || access.CanModerate),
            children);

        var threads = topLevel.ConvertAll(c => ToDto(c, [.. replies.Where(r => r.ParentId == c.Id).Select(r => ToDto(r, []))]));
        var commentCount = await db.NewsComments.CountAsync(c => c.ArticleId == articleId && !c.IsDeleted, cancellationToken);
        return new NewsDiscussion(SummaryOf(null), threads, commentCount, hasMore, access.CanReact, access.CanComment);
    }

    /// <inheritdoc />
    public async Task<Result<ReactionSummary>> ReactAsync(Guid articleId, Guid? commentId, ReactionKind kind, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(articleId, cancellationToken);
        if (access is null || currentUser.Id is not { } me)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        if (!access.CanReact || !Enum.IsDefined(kind))
        {
            return Error.Validation(Texts.T("Реакції доступні лише для опублікованих новин."));
        }

        if (commentId is { } target && !await db.NewsComments.AnyAsync(c => c.Id == target && c.ArticleId == articleId && !c.IsDeleted, cancellationToken))
        {
            return Error.NotFound(Texts.T("Коментар не знайдено."));
        }

        var existing = await db.NewsReactions.FirstOrDefaultAsync(r => r.ArticleId == articleId && r.CommentId == commentId && r.UserId == me, cancellationToken);
        if (existing is null)
        {
            db.NewsReactions.Add(new NewsReaction(articleId, commentId, me, kind, timeProvider.GetUtcNow()));
        }
        else if (existing.Kind == kind)
        {
            db.NewsReactions.Remove(existing);
        }
        else
        {
            existing.Change(kind, timeProvider.GetUtcNow());
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A double click from two tabs: the other request has already saved the reaction.
            db.DiscardChanges();
        }

        events.Publish(articleId);
        var counts = await db.NewsReactions
            .Where(r => r.ArticleId == articleId && r.CommentId == commentId)
            .GroupBy(static r => r.Kind)
            .Select(static g => new ReactionCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
        var mine = await db.NewsReactions
            .Where(r => r.ArticleId == articleId && r.CommentId == commentId && r.UserId == me)
            .Select(static r => (ReactionKind?)r.Kind)
            .FirstOrDefaultAsync(cancellationToken);
        return ReactionSummary.From(counts, mine);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CommentAsync(Guid articleId, Guid? parentId, string body, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(articleId, cancellationToken);
        if (access is null || currentUser.Id is not { } me)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        if (!access.CanComment)
        {
            return Error.Validation(Texts.T("Коментарі до цієї новини вимкнено."));
        }

        Guid? threadId = null;
        if (parentId is { } replyTo)
        {
            var parent = await db.NewsComments.AsNoTracking()
                .Where(c => c.Id == replyTo && c.ArticleId == articleId)
                .Select(static c => new { c.Id, c.ParentId })
                .FirstOrDefaultAsync(cancellationToken);
            if (parent is null)
            {
                return Error.NotFound(Texts.T("Коментар не знайдено."));
            }

            // One level of replies: a reply to a reply joins the thread of the top-level comment.
            threadId = parent.ParentId ?? parent.Id;
        }

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var comment = new NewsComment(articleId, me, threadId, body);
            db.NewsComments.Add(comment);
            await db.SaveChangesAsync(cancellationToken);
            events.Publish(articleId);
            return comment.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> EditCommentAsync(Guid commentId, string body, CancellationToken cancellationToken)
    {
        var comment = await db.NewsComments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is null || comment.IsDeleted || await AccessAsync(comment.ArticleId, cancellationToken) is null)
        {
            return Error.NotFound(Texts.T("Коментар не знайдено."));
        }

        if (comment.AuthorId != currentUser.Id)
        {
            return Error.Forbidden(Texts.T("Редагувати можна лише власні коментарі."));
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            comment.Edit(body, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            events.Publish(comment.ArticleId);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await db.NewsComments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        var access = comment is null || comment.IsDeleted ? null : await AccessAsync(comment.ArticleId, cancellationToken);
        if (comment is null || access is null)
        {
            return Error.NotFound(Texts.T("Коментар не знайдено."));
        }

        var own = comment.AuthorId == currentUser.Id;
        if (!own && !access.CanModerate)
        {
            return Error.Forbidden(Texts.T("Видаляти чужі коментарі можуть лише редактори новин."));
        }

        if (!own)
        {
            var excerpt = comment.Body.Length > 200 ? comment.Body[..200] + "…" : comment.Body;
            audit.Add(AuditActions.NewsCommentDeleted, nameof(NewsComment), comment.Id.ToString(), $"Видалено коментар до новини: «{excerpt}»");
        }

        // A comment with replies stays as a placeholder of its thread; otherwise nothing of it is kept.
        db.NewsReactions.RemoveRange(await db.NewsReactions.Where(r => r.CommentId == commentId).ToListAsync(cancellationToken));
        if (await db.NewsComments.AnyAsync(c => c.ParentId == commentId, cancellationToken))
        {
            comment.Delete();
        }
        else
        {
            db.NewsComments.Remove(comment);
        }

        await db.SaveChangesAsync(cancellationToken);
        events.Publish(comment.ArticleId);
        return Result.Success();
    }

    /// <summary>
    /// Projects comments for display (filtering happens before, on the entity).
    /// </summary>
    /// <param name="comments">The filtered and ordered comments.</param>
    /// <returns>The rows.</returns>
    private static IQueryable<CommentRow> Rows(IQueryable<NewsComment> comments) => comments
        .Select(static c => new CommentRow(
            c.Id,
            c.ParentId,
            c.AuthorId,
            c.Author!.LastName,
            c.Author.FirstName,
            c.Author.MiddleName,
            c.Author.PhotoId,
            c.Body,
            c.CreatedAt,
            c.EditedAt,
            c.IsDeleted));

    /// <summary>
    /// Builds the full name of a comment author.
    /// </summary>
    /// <param name="row">The comment row.</param>
    /// <returns>The full name.</returns>
    private static string FullName(CommentRow row)
        => row.MiddleName is null ? $"{row.LastName} {row.FirstName}" : $"{row.LastName} {row.FirstName} {row.MiddleName}";

    /// <summary>
    /// Checks that the current user may read an article and returns what they may do with its discussion.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The permissions, or <see langword="null"/> when the article is not readable.</returns>
    private async Task<DiscussionAccess?> AccessAsync(Guid articleId, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.View))
        {
            return null;
        }

        var article = await db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);
        if (article is null)
        {
            return null;
        }

        var canManage = currentUser.Has(Permissions.News.Manage)
            && NewsService.CanManage(article, await accessScope.GetAdministrationScopeAsync(Permissions.News.Manage, cancellationToken));
        if (!article.IsPublished && !canManage)
        {
            return null;
        }

        return new DiscussionAccess(article.IsPublished, article.IsPublished && article.AllowComments, canManage);
    }

    /// <summary>
    /// What the current user may do with a discussion.
    /// </summary>
    /// <param name="CanReact">Whether reactions are possible (the article is published).</param>
    /// <param name="CanComment">Whether comments are open.</param>
    /// <param name="CanModerate">Whether the user manages the article's news and may delete any comment.</param>
    private sealed record DiscussionAccess(bool CanReact, bool CanComment, bool CanModerate);

    /// <summary>
    /// A comment projected from the database.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="ParentId">The parent comment.</param>
    /// <param name="AuthorId">The author.</param>
    /// <param name="LastName">The author's last name.</param>
    /// <param name="FirstName">The author's first name.</param>
    /// <param name="MiddleName">The author's patronymic.</param>
    /// <param name="PhotoId">The author's photo.</param>
    /// <param name="Body">The text.</param>
    /// <param name="CreatedAt">The moment of writing.</param>
    /// <param name="EditedAt">The moment of the last edit.</param>
    /// <param name="IsDeleted">Whether the comment was deleted.</param>
    private sealed record CommentRow(
        Guid Id,
        Guid? ParentId,
        Guid AuthorId,
        string LastName,
        string FirstName,
        string? MiddleName,
        Guid? PhotoId,
        string Body,
        DateTimeOffset CreatedAt,
        DateTimeOffset? EditedAt,
        bool IsDeleted);
}
