using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Replaces the text of the current user's comment.
/// </summary>
public static class EditNewsComment
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    /// <param name="CommentId">The comment.</param>
    /// <param name="Body">The new text.</param>
    public sealed record Command(Guid ArticleId, Guid CommentId, string Body) : ICommand<Result>;

    /// <summary>
    /// Saves the new text of an own comment.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="access">The discussion rights.</param>
    /// <param name="events">Tells the open pages of the article.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, DiscussionAccess access, INewsDiscussionEvents events, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var comment = await db.NewsComments.FirstOrDefaultAsync(c => c.Id == request.CommentId && c.ArticleId == request.ArticleId, cancellationToken);
            if (comment is null || comment.IsDeleted || await access.OfAsync(comment.ArticleId, cancellationToken) is null)
            {
                return DiscussionAccess.CommentNotFound();
            }

            if (comment.AuthorId != currentUser.Id)
            {
                return Error.Forbidden(Texts.T("Редагувати можна лише власні коментарі."));
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                comment.Edit(request.Body, timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                events.Publish(comment.ArticleId);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/news/{articleId}/comments/{commentId}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DiscussionAccess.MapDiscussion(api).MapPut("comments/{commentId:guid}", HandleAsync);

        /// <summary>
        /// Replaces the text of the current user's comment.
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="commentId">The comment.</param>
        /// <param name="comment">The new text.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid articleId,
            Guid commentId,
            CommentRequest comment,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(articleId, commentId, comment.Body), cancellationToken)).ToNoContent();
    }
}
