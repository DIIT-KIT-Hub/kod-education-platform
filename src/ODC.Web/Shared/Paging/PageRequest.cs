using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ODC.Web.Shared.Paging;

/// <summary>
/// Common paging, search and sorting parameters of a data grid, bound from the query string. The bound properties are
/// nullable so every parameter is optional for minimal APIs (<c>[AsParameters]</c> treats a non-nullable value type as
/// required); code reads the normalized <see cref="CurrentPage"/>, <see cref="CurrentPageSize"/> and
/// <see cref="IsDescending"/>.
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
    public int? Page { get; init => field = value is null ? null : Math.Max(1, value.Value); }

    /// <summary>
    /// Gets the page size (clamped to 1..<see cref="MaxPageSize"/>).
    /// </summary>
    public int? PageSize { get; init => field = value is null ? null : Math.Clamp(value.Value, 1, MaxPageSize); }

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
    public bool? Descending { get; init; }

    /// <summary>
    /// Gets the 1-based page number (1 when not given).
    /// </summary>
    [BindNever]
    public int CurrentPage => Page ?? 1;

    /// <summary>
    /// Gets the page size (<see cref="DefaultPageSize"/> when not given).
    /// </summary>
    [BindNever]
    public int CurrentPageSize => PageSize ?? DefaultPageSize;

    /// <summary>
    /// Gets a value indicating whether sorting is descending (false when not given).
    /// </summary>
    [BindNever]
    public bool IsDescending => Descending ?? false;
}
