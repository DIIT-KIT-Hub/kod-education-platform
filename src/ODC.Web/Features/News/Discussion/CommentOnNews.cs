using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Adds a comment to an article or a reply to any comment (at most 50 levels deep); the author of the comment answered
/// gets a notification.
/// </summary>
public static class CommentOnNews
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    /// <param name="ParentId">The comment answered, or <see langword="null"/> for a top-level comment.</param>
    /// <param name="Body">The text.</param>
    public sealed record Command(Guid ArticleId, Guid? ParentId, string Body) : ICommand<Result<Guid>>;

    /// <summary>
    /// Saves the comment, tells the article's open pages and the author answered.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="access">The discussion rights.</param>
    /// <param name="events">Tells the open pages of the article.</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, DiscussionAccess access, INewsDiscussionEvents events)
        : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var articleId = request.ArticleId;
            var rights = await access.OfAsync(articleId, cancellationToken);
            if (rights is null || currentUser.Id is not { } me)
            {
                return DiscussionAccess.ArticleNotFound();
            }

            if (!rights.CanComment)
            {
                return Error.Validation(Texts.T("Коментарі до цієї новини вимкнено."));
            }

            NewsComment? parent = null;
            if (request.ParentId is { } replyTo)
            {
                parent = await db.NewsComments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == replyTo && c.ArticleId == articleId, cancellationToken);
                if (parent is null)
                {
                    return DiscussionAccess.CommentNotFound();
                }
            }

            await access.MakePresentAsync(cancellationToken);
            return await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var comment = new NewsComment(articleId, me, parent, request.Body);
                db.NewsComments.Add(comment);
                await db.SaveChangesAsync(cancellationToken);
                events.Publish(articleId);
                return comment.Id;
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/news/{articleId}/comments</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DiscussionAccess.MapDiscussion(api).MapPost("comments", HandleAsync);

        /// <summary>
        /// Adds a comment or a reply.
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            Guid articleId,
            CommentRequest comment,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(articleId, comment.ParentId, comment.Body), cancellationToken))
                .ToCreated(_ => $"{ApiEndpoints.Base}/news/{articleId}/discussion");
    }
}
