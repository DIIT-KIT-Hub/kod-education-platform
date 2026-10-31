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
    /// The default number of comment threads per page.
    /// </summary>
    const int PageSize = 10;

    /// <summary>
    /// The largest number of comment threads per page.
    /// </summary>
    const int MaxPageSize = 50;

    /// <summary>
    /// Gets the reactions to an article and one page of its comment threads, each with the whole branch of replies.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="page">The 1-based page of threads.</param>
    /// <param name="pageSize">The number of threads per page.</param>
    /// <param name="sort">The order of the threads.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The discussion.</returns>
    Task<Result<NewsDiscussion>> GetAsync(Guid articleId, int page, int pageSize, CommentSort sort, CancellationToken cancellationToken);

    /// <summary>
    /// Checks that the current user may read the discussion of an article (e.g. before following its changes).
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the article is readable.</returns>
    Task<bool> CanReadAsync(Guid articleId, CancellationToken cancellationToken);

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
    /// Adds a top-level comment or a reply to any comment of the article (the reply goes under that very comment).
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
    /// <param name="articleId">The article the comment belongs to.</param>
    /// <param name="commentId">The comment.</param>
    /// <param name="body">The new text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> EditCommentAsync(Guid articleId, Guid commentId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a comment of the current user or, for a moderator, anyone's comment.
    /// </summary>
    /// <param name="articleId">The article the comment belongs to.</param>
    /// <param name="commentId">The comment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteCommentAsync(Guid articleId, Guid commentId, CancellationToken cancellationToken);
}
