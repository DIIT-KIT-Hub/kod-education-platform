namespace ODC.Web.Features.News.Domain;

/// <summary>
/// The index of the published news of the whole cabinet (master database): such an article lives in the database of
/// the organization it was written in, and every other organization lists it from there. Written after the article is
/// saved and rebuilt at every start, so a lost write only delays an article until the next start.
/// </summary>
public sealed class GeneralArticle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralArticle"/> class.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="organizationId">The organization whose database holds it.</param>
    /// <param name="publishedAt">The publication moment.</param>
    public GeneralArticle(Guid articleId, Guid organizationId, DateTimeOffset publishedAt)
    {
        ArticleId = articleId;
        OrganizationId = organizationId;
        PublishedAt = publishedAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralArticle"/> class for EF Core materialization.
    /// </summary>
    private GeneralArticle()
    {
    }

    /// <summary>
    /// Gets the article.
    /// </summary>
    public Guid ArticleId { get; private init; }

    /// <summary>
    /// Gets the organization whose database holds it.
    /// </summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>
    /// Gets the publication moment.
    /// </summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>
    /// Follows a new publication moment.
    /// </summary>
    /// <param name="publishedAt">The moment.</param>
    public void Republish(DateTimeOffset publishedAt) => PublishedAt = publishedAt;
}
