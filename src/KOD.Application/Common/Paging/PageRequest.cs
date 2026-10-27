namespace KOD.Application.Common.Paging;

/// <summary>
/// Common paging, search and sorting parameters of a data grid.
/// </summary>
public record PageRequest
{
    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// The maximum page size accepted from clients.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int Page { get; init => field = Math.Max(1, value); } = 1;

    /// <summary>
    /// Gets the page size (clamped to 1..<see cref="MaxPageSize"/>).
    /// </summary>
    public int PageSize { get; init => field = Math.Clamp(value, 1, MaxPageSize); } = DefaultPageSize;

    /// <summary>
    /// Gets the free-text search term.
    /// </summary>
    public string? Search { get; init => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }

    /// <summary>
    /// Gets the sort key (grid-specific, unknown keys fall back to the default order).
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether sorting is descending.
    /// </summary>
    public bool Descending { get; init; }
}
