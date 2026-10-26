namespace KOD.Domain.News;

/// <summary>
/// Records that a reader opened an article (once per reader), for the view counter.
/// </summary>
/// <param name="articleId">The article.</param>
/// <param name="userId">The reader.</param>
/// <param name="viewedAt">The moment of the first view.</param>
public sealed class NewsView(Guid articleId, Guid userId, DateTimeOffset viewedAt)
{
    /// <summary>
    /// Gets the article.
    /// </summary>
    public Guid ArticleId { get; private set; } = articleId;

    /// <summary>
    /// Gets the reader.
    /// </summary>
    public Guid UserId { get; private set; } = userId;

    /// <summary>
    /// Gets the moment of the first view.
    /// </summary>
    public DateTimeOffset ViewedAt { get; private set; } = viewedAt;
}
