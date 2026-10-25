using KOD.Application.Common.Files;
using KOD.Domain.News;

namespace KOD.Application.Features.News;

/// <summary>
/// Creates or updates a news article.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Summary">The lead.</param>
/// <param name="Body">The Markdown body.</param>
/// <param name="Category">The category.</param>
/// <param name="IsPinned">Whether the article is pinned.</param>
/// <param name="Publish">Whether the article is published.</param>
/// <param name="CoverImage">A new cover image, if any.</param>
/// <param name="RemoveCover">Whether to remove the existing cover image.</param>
/// <param name="OrganizationId">The organization of the article; <see langword="null"/> for news of the whole cabinet.</param>
public sealed record SaveNewsCommand(
    string Title,
    string Summary,
    string Body,
    NewsCategory Category,
    bool IsPinned,
    bool Publish,
    FileUpload? CoverImage,
    bool RemoveCover,
    Guid? OrganizationId);
