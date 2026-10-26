using KOD.Domain.Common;
using KOD.Domain.Organization;
using KOD.Domain.Users;

namespace KOD.Domain.News;

/// <summary>
/// A news article or announcement.
/// </summary>
public sealed class NewsArticle : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    /// Maximum length of <see cref="Summary"/>.
    /// </summary>
    public const int SummaryMaxLength = 500;

    /// <summary>
    /// Maximum length of <see cref="Body"/> (HTML markup included).
    /// </summary>
    public const int BodyMaxLength = 200_000;

    /// <summary>
    /// Maximum length of <see cref="Slug"/>.
    /// </summary>
    public const int SlugMaxLength = 160;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsArticle"/> class as a draft.
    /// </summary>
    /// <param name="slug">The unique URL-friendly identifier.</param>
    /// <param name="title">The title.</param>
    /// <param name="summary">The short lead shown in lists.</param>
    /// <param name="body">The body.</param>
    /// <param name="format">The markup of <paramref name="body"/>.</param>
    /// <param name="category">The category.</param>
    /// <param name="authorId">The author's user identifier.</param>
    public NewsArticle(string slug, string title, string summary, string body, NewsBodyFormat format, NewsCategory category, Guid authorId)
    {
        Slug = Guard.Text(slug, SlugMaxLength, "Адреса");
        Title = Guard.Text(title, TitleMaxLength, "Заголовок");
        Summary = Guard.Text(summary, SummaryMaxLength, "Анонс");
        Body = Guard.Text(body, BodyMaxLength, "Текст");
        BodyFormat = format;
        Category = category;
        AuthorId = authorId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsArticle"/> class for EF Core materialization.
    /// </summary>
    private NewsArticle()
    {
        Slug = string.Empty;
        Title = string.Empty;
        Summary = string.Empty;
        Body = string.Empty;
    }

    /// <summary>
    /// Gets the unique URL-friendly identifier.
    /// </summary>
    public string Slug { get; private set; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the short lead shown in lists.
    /// </summary>
    public string Summary { get; private set; }

    /// <summary>
    /// Gets the body: sanitized HTML of the rich text editor, or Markdown without raw HTML (see <see cref="BodyFormat"/>).
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Gets the markup of <see cref="Body"/>.
    /// </summary>
    public NewsBodyFormat BodyFormat { get; private set; }

    /// <summary>
    /// Gets a value indicating whether readers may comment on the article (reactions are always allowed).
    /// </summary>
    public bool AllowComments { get; private set; } = true;

    /// <summary>
    /// Gets the category.
    /// </summary>
    public NewsCategory Category { get; private set; }

    /// <summary>
    /// Gets the public URL of the cover image, if any.
    /// </summary>
    public string? CoverImageUrl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the article is pinned to the top of the feed.
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// Gets the publication moment; <see langword="null"/> for drafts.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the article is published.
    /// </summary>
    public bool IsPublished => PublishedAt is not null;

    /// <summary>
    /// Gets the author's user identifier.
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets the organization the article belongs to; <see langword="null"/> for news of the whole cabinet.
    /// </summary>
    public Guid? OrganizationId { get; private set; }

    /// <summary>
    /// Gets the organization the article belongs to.
    /// </summary>
    public OrganizationUnit? Organization { get; private set; }

    /// <summary>
    /// Updates the content.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="summary">The lead.</param>
    /// <param name="html">The sanitized HTML body (an edited article is always stored as HTML).</param>
    /// <param name="category">The category.</param>
    /// <param name="isPinned">Whether the article is pinned.</param>
    /// <param name="allowComments">Whether readers may comment.</param>
    public void Update(string title, string summary, string html, NewsCategory category, bool isPinned, bool allowComments)
    {
        Title = Guard.Text(title, TitleMaxLength, "Заголовок");
        Summary = Guard.Text(summary, SummaryMaxLength, "Анонс");
        Body = Guard.Text(html, BodyMaxLength, "Текст");
        BodyFormat = NewsBodyFormat.Html;
        Category = category;
        IsPinned = isPinned;
        AllowComments = allowComments;
    }

    /// <summary>
    /// Pins the article to the top of the feed or unpins it.
    /// </summary>
    /// <param name="isPinned">Whether the article is pinned.</param>
    public void Pin(bool isPinned) => IsPinned = isPinned;

    /// <summary>
    /// Assigns the article to an organization.
    /// </summary>
    /// <param name="organizationId">The organization, or <see langword="null"/> for news of the whole cabinet.</param>
    public void AssignTo(Guid? organizationId) => OrganizationId = organizationId;

    /// <summary>
    /// Sets or removes the cover image.
    /// </summary>
    /// <param name="url">The public URL of the image.</param>
    public void SetCover(string? url) => CoverImageUrl = url;

    /// <summary>
    /// Publishes the article (keeps the original publication moment when republishing).
    /// </summary>
    /// <param name="moment">The current moment.</param>
    public void Publish(DateTimeOffset moment) => PublishedAt ??= moment;

    /// <summary>
    /// Returns the article to drafts.
    /// </summary>
    public void Unpublish() => PublishedAt = null;
}
