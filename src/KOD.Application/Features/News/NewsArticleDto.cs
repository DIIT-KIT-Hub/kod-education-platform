using KOD.Application.Common.Models;
using KOD.Domain.News;

namespace KOD.Application.Features.News;

/// <summary>
/// A news article.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Slug">The slug.</param>
/// <param name="Title">The title.</param>
/// <param name="Summary">The lead.</param>
/// <param name="Body">The stored body (see <paramref name="BodyFormat"/>).</param>
/// <param name="BodyFormat">The markup of <paramref name="Body"/>.</param>
/// <param name="BodyHtml">The safe HTML of the body, also the starting content of the editor.</param>
/// <param name="Category">The category.</param>
/// <param name="CoverImageUrl">The cover image URL.</param>
/// <param name="IsPinned">Whether the article is pinned.</param>
/// <param name="PublishedAt">The publication moment.</param>
/// <param name="Author">The author.</param>
/// <param name="UpdatedAt">The last change.</param>
/// <param name="Organization">The organization the article belongs to; <see langword="null"/> for news of the whole cabinet.</param>
/// <param name="AllowComments">Whether readers may comment.</param>
/// <param name="ViewCount">How many readers opened the article.</param>
/// <param name="CanManage">Whether the current user may edit the article and moderate its comments.</param>
public sealed record NewsArticleDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    NewsBodyFormat BodyFormat,
    string BodyHtml,
    NewsCategory Category,
    string? CoverImageUrl,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    UserRef Author,
    DateTimeOffset? UpdatedAt,
    UnitRef? Organization,
    bool AllowComments,
    int ViewCount,
    bool CanManage);
