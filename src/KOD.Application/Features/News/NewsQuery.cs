using KOD.Application.Common.Paging;
using KOD.Domain.News;

namespace KOD.Application.Features.News;

/// <summary>
/// Paging and filter parameters of the news feed.
/// </summary>
public sealed record NewsQuery : PageRequest
{
    /// <summary>
    /// Gets the category filter.
    /// </summary>
    public NewsCategory? Category { get; init; }

    /// <summary>
    /// Gets a value indicating whether drafts are included (management view).
    /// </summary>
    public bool IncludeDrafts { get; init; }
}
