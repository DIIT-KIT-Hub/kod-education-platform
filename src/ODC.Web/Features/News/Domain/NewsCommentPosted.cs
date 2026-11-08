using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.News.Domain;

/// <summary>
/// Somebody commented on a news article or answered a comment.
/// </summary>
/// <param name="ArticleId">The article.</param>
/// <param name="CommentId">The new comment.</param>
/// <param name="AuthorId">The writer.</param>
/// <param name="ParentAuthorId">The author of the comment answered, or <see langword="null"/> for a comment on the article.</param>
public sealed record NewsCommentPosted(Guid ArticleId, Guid CommentId, Guid AuthorId, Guid? ParentAuthorId) : IDomainEvent;
