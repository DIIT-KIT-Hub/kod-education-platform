using KOD.Domain.News;

namespace KOD.Application.Features.News;

/// <summary>
/// A full news article.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Slug">The URL identifier.</param>
/// <param name="Title">The title.</param>
/// <param name="Summary">The lead.</param>
/// <param name="Body">The Markdown body.</param>
/// <param name="BodyHtml">The rendered safe HTML body.</param>
/// <param name="Category">The category.</param>
/// <param name="CoverImageUrl">The cover image URL.</param>
/// <param name="IsPinned">Whether the article is pinned.</param>
/// <param name="PublishedAt">The publication moment.</param>
/// <param name="AuthorName">The author's name.</param>
/// <param name="UpdatedAt">The last modification moment.</param>
public sealed record NewsArticleDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    string BodyHtml,
    NewsCategory Category,
    string? CoverImageUrl,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    string AuthorName,
    DateTimeOffset? UpdatedAt);
