using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Deletes a comment (the author's own, or anyone's for a news manager of the article). A comment with replies stays as
/// a placeholder of its thread and goes away with its last reply.
/// </summary>
public static class DeleteNewsComment
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    /// <param name="CommentId">The comment.</param>
    public sealed record Command(Guid ArticleId, Guid CommentId) : ICommand<Result>;

    /// <summary>
    /// Deletes the comment and the placeholders it kept.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="access">The discussion rights.</param>
    /// <param name="events">Tells the open pages of the article.</param>
    /// <param name="audit">The audit logger (moderation).</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, DiscussionAccess access, INewsDiscussionEvents events, IAuditLogger audit)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var commentId = request.CommentId;
            var comment = await db.NewsComments.FirstOrDefaultAsync(c => c.Id == commentId && c.ArticleId == request.ArticleId, cancellationToken);
            var rights = comment is null || comment.IsDeleted ? null : await access.OfAsync(comment.ArticleId, cancellationToken);
            if (comment is null || rights is null)
            {
                return DiscussionAccess.CommentNotFound();
            }

            var own = comment.AuthorId == currentUser.Id;
            if (!own && !rights.CanModerate)
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

            // A deleted comment kept only for this reply goes too once its branch is empty, up the thread.
            var parentId = comment.ParentId;
            while (parentId is { } id)
            {
                var parent = await db.NewsComments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (parent is null || !parent.IsDeleted || await db.NewsComments.AnyAsync(c => c.ParentId == id, cancellationToken))
                {
                    break;
                }

                parentId = parent.ParentId;
                db.NewsComments.Remove(parent);
                await db.SaveChangesAsync(cancellationToken);
            }

            events.Publish(comment.ArticleId);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/news/{articleId}/comments/{commentId}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DiscussionAccess.MapDiscussion(api).MapDelete("comments/{commentId:guid}", HandleAsync);

        /// <summary>
        /// Deletes a comment (the author's own, or anyone's for a news manager of the article).
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="commentId">The comment.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid articleId, Guid commentId, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(articleId, commentId), cancellationToken)).ToNoContent();
    }
}
