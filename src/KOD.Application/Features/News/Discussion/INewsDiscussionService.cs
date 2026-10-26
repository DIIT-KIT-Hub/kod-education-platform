using KOD.Application.Common.Results;
using KOD.Domain.News;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// Reactions and comments under news articles. Every reader of the news may react and comment on published articles
/// (when the article allows comments); authors edit and delete their comments, news managers of the article's
/// organization delete any comment.
/// </summary>
public interface INewsDiscussionService
{
    /// <summary>
    /// The default number of top-level comments loaded at once.
    /// </summary>
    const int PageSize = 20;

    /// <summary>
    /// Gets the reactions and the newest comments of an article.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="limit">How many top-level comments to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The discussion.</returns>
    Task<Result<NewsDiscussion>> GetAsync(Guid articleId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the current user's emotion on the article or a comment; choosing the same emotion again removes it.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="commentId">The comment, or <see langword="null"/> for the article itself.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reactions to the target.</returns>
    Task<Result<ReactionSummary>> ReactAsync(Guid articleId, Guid? commentId, ReactionKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a comment or a reply (a reply to a reply joins the same thread).
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="parentId">The comment to reply to, if any.</param>
    /// <param name="body">The plain text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifier of the new comment.</returns>
    Task<Result<Guid>> CommentAsync(Guid articleId, Guid? parentId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the text of the current user's comment.
    /// </summary>
    /// <param name="commentId">The comment.</param>
    /// <param name="body">The new text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> EditCommentAsync(Guid commentId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a comment of the current user or, for a moderator, anyone's comment.
    /// </summary>
    /// <param name="commentId">The comment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken);
}
