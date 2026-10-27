using KOD.Application.Common.Media;
using KOD.Application.Common.Models;
using KOD.Domain.News;

namespace KOD.Application.Features.News;

/// <summary>
/// A news card in the feed.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Slug">The URL identifier.</param>
/// <param name="Title">The title.</param>
/// <param name="Summary">The lead.</param>
/// <param name="Category">The category.</param>
/// <param name="CoverImageUrl">The cover image URL.</param>
/// <param name="IsPinned">Whether the article is pinned.</param>
/// <param name="PublishedAt">The publication moment; <see langword="null"/> for drafts.</param>
/// <param name="AuthorName">The author's name.</param>
/// <param name="AuthorPhotoId">The author's profile photo.</param>
/// <param name="UpdatedAt">The last modification moment.</param>
/// <param name="Organization">The organization of the article; <see langword="null"/> for news of the whole cabinet.</param>
/// <param name="CommentCount">The number of comments (deleted ones excluded).</param>
/// <param name="ReactionCount">The number of reactions to the article.</param>
/// <param name="ViewCount">The number of readers who opened the article.</param>
public sealed record NewsListItem(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    NewsCategory Category,
    string? CoverImageUrl,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    string AuthorName,
    Guid? AuthorPhotoId,
    DateTimeOffset? UpdatedAt,
    UnitRef? Organization,
    int CommentCount,
    int ReactionCount,
    int ViewCount)
{
    /// <summary>
    /// Gets the URL of a small cover for cards (the thumbnail of the cover image).
    /// </summary>
    public string? CoverThumbnailUrl => MediaUrls.TryParse(CoverImageUrl, out var id) ? MediaUrls.ThumbnailOf(id) : null;
}
