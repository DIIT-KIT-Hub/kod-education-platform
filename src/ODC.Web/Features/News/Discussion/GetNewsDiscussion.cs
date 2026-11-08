using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// The reactions to an article and one page of its comment threads, each with its whole branch of replies (Reddit-like):
/// two queries, the tree is built in memory.
/// </summary>
public static class GetNewsDiscussion
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    /// <param name="Page">The 1-based page of threads.</param>
    /// <param name="PageSize">The threads per page.</param>
    /// <param name="Sort">The order of the threads.</param>
    public sealed record Query(Guid ArticleId, int Page, int PageSize, CommentSort Sort) : IQuery<Result<NewsDiscussion>>;

    /// <summary>
    /// Reads the page of threads.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="access">The discussion rights.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, DiscussionAccess access) : IRequestHandler<Query, Result<NewsDiscussion>>
    {
        /// <inheritdoc />
        public async Task<Result<NewsDiscussion>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var articleId = request.ArticleId;
            var rights = await access.OfAsync(articleId, cancellationToken);
            if (rights is null)
            {
                return DiscussionAccess.ArticleNotFound();
            }

            var me = currentUser.Id;
            var pageSize = Math.Clamp(request.PageSize, 1, DiscussionAccess.MaxPageSize);
            var page = Math.Max(1, request.Page);

            // A deleted top-level comment stays only as a placeholder of a branch that still has replies.
            var threads = db.NewsComments
                .Where(c => c.ArticleId == articleId && c.ParentId == null)
                .Where(c => !c.IsDeleted || db.NewsComments.Any(r => r.RootId == c.Id && r.Id != c.Id && !r.IsDeleted));
            var threadCount = await threads.CountAsync(cancellationToken);
            var ordered = request.Sort switch
            {
                CommentSort.Oldest => threads.OrderBy(static c => c.CreatedAt),
                CommentSort.Newest => threads.OrderByDescending(static c => c.CreatedAt),
                _ => threads
                    .OrderByDescending(c => db.NewsReactions.Count(r => r.CommentId == c.Id)
                        + db.NewsComments.Count(r => r.RootId == c.Id && r.Id != c.Id && !r.IsDeleted))
                    .ThenByDescending(static c => c.CreatedAt),
            };
            var roots = await Rows(ordered.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(cancellationToken);

            // Every branch of the page in one query: the replies know the top-level comment of their thread.
            var rootIds = roots.ConvertAll(static c => c.Id);
            var replies = await Rows(db.NewsComments
                    .Where(c => c.ArticleId == articleId && c.ParentId != null && rootIds.Contains(c.RootId))
                    .OrderBy(static c => c.CreatedAt))
                .ToListAsync(cancellationToken);

            var commentIds = rootIds.Concat(replies.Select(static r => r.Id)).ToList();
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

            var children = replies.ToLookup(static r => r.ParentId);
            var authors = roots.Concat(replies).ToDictionary(static r => r.Id, static r => new UserRef(r.AuthorId, FullName(r), r.PhotoId));

            // Builds a branch; a deleted comment is kept only while replies under it remain.
            NewsCommentDto? Build(CommentRow row)
            {
                var branch = children[row.Id].Select(Build).OfType<NewsCommentDto>().ToList();
                if (row.IsDeleted && branch.Count == 0)
                {
                    return null;
                }

                return new NewsCommentDto(
                    row.Id,
                    row.ParentId,
                    row.ParentId is { } parent ? authors.GetValueOrDefault(parent) : null,
                    row.Depth,
                    authors[row.Id],
                    row.IsDeleted ? string.Empty : row.Body,
                    row.CreatedAt,
                    row.EditedAt,
                    row.IsDeleted,
                    SummaryOf(row.Id),
                    !row.IsDeleted && row.AuthorId == me,
                    !row.IsDeleted && (row.AuthorId == me || rights.CanModerate),
                    branch.Sum(static reply => reply.ReplyCount + (reply.IsDeleted ? 0 : 1)),
                    branch);
            }

            var comments = roots.Select(Build).OfType<NewsCommentDto>().ToList();
            var commentCount = await db.NewsComments.CountAsync(c => c.ArticleId == articleId && !c.IsDeleted, cancellationToken);
            return new NewsDiscussion(SummaryOf(null), comments, commentCount, threadCount, page, pageSize, request.Sort, rights.CanReact, rights.CanComment);
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
                c.Depth,
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
    }

    /// <summary>
    /// <c>GET /api/v1/news/{articleId}/discussion</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DiscussionAccess.MapDiscussion(api).MapGet("discussion", HandleAsync);

        /// <summary>
        /// Gets the reactions to an article and one page of its comment threads, each with its whole branch of replies.
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="page">The 1-based page of threads.</param>
        /// <param name="pageSize">The threads per page.</param>
        /// <param name="sort">The order of the threads.</param>
        /// <returns>The discussion.</returns>
        private static async Task<Results<Ok<NewsDiscussion>, ProblemHttpResult>> HandleAsync(
            Guid articleId,
            IRequestHandler<Query, Result<NewsDiscussion>> handler,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = DiscussionAccess.PageSize,
            CommentSort sort = CommentSort.Best)
            => (await handler.HandleAsync(new Query(articleId, page, pageSize, sort), cancellationToken)).ToOk();
    }

    /// <summary>
    /// A comment projected from the database.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="ParentId">The parent comment.</param>
    /// <param name="Depth">The nesting level.</param>
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
        int Depth,
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
