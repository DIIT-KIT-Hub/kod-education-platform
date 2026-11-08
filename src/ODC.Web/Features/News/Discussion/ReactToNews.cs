using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Sets the current user's reaction to an article or one of its comments; the same reaction again removes it.
/// </summary>
public static class ReactToNews
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    /// <param name="CommentId">The comment, or <see langword="null"/> for the article itself.</param>
    /// <param name="Kind">The reaction.</param>
    public sealed record Command(Guid ArticleId, Guid? CommentId, ReactionKind Kind) : ICommand<Result<ReactionSummary>>;

    /// <summary>
    /// Adds, changes or removes the reaction and tells the article's open pages.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="access">The discussion rights.</param>
    /// <param name="events">Tells the open pages of the article.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, DiscussionAccess access, INewsDiscussionEvents events, TimeProvider timeProvider)
        : IRequestHandler<Command, Result<ReactionSummary>>
    {
        /// <inheritdoc />
        public async Task<Result<ReactionSummary>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var (articleId, commentId, kind) = (request.ArticleId, request.CommentId, request.Kind);
            var rights = await access.OfAsync(articleId, cancellationToken);
            if (rights is null || currentUser.Id is not { } me)
            {
                return DiscussionAccess.ArticleNotFound();
            }

            if (!rights.CanReact || !Enum.IsDefined(kind))
            {
                return Error.Validation(Texts.T("Реакції доступні лише для опублікованих новин."));
            }

            if (commentId is { } target && !await db.NewsComments.AnyAsync(c => c.Id == target && c.ArticleId == articleId && !c.IsDeleted, cancellationToken))
            {
                return DiscussionAccess.CommentNotFound();
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
    }

    /// <summary>
    /// <c>POST /api/v1/news/{articleId}/reactions/{kind}</c> and <c>…/comments/{commentId}/reactions/{kind}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var discussion = DiscussionAccess.MapDiscussion(api);
            discussion.MapPost("reactions/{kind}", ArticleAsync);
            discussion.MapPost("comments/{commentId:guid}/reactions/{kind}", CommentAsync);
        }

        /// <summary>
        /// Sets the current user's reaction to the article; the same reaction again removes it.
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="kind">The reaction.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The reactions to the article.</returns>
        private static async Task<Results<Ok<ReactionSummary>, ProblemHttpResult>> ArticleAsync(
            Guid articleId,
            ReactionKind kind,
            IRequestHandler<Command, Result<ReactionSummary>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(articleId, null, kind), cancellationToken)).ToOk();

        /// <summary>
        /// Sets the current user's reaction to a comment; the same reaction again removes it.
        /// </summary>
        /// <param name="articleId">The article.</param>
        /// <param name="commentId">The comment.</param>
        /// <param name="kind">The reaction.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The reactions to the comment.</returns>
        private static async Task<Results<Ok<ReactionSummary>, ProblemHttpResult>> CommentAsync(
            Guid articleId,
            Guid commentId,
            ReactionKind kind,
            IRequestHandler<Command, Result<ReactionSummary>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(articleId, commentId, kind), cancellationToken)).ToOk();
    }
}
