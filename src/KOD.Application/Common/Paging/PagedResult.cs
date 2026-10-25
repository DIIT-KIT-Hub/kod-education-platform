namespace KOD.Application.Common.Paging;

/// <summary>
/// A single page of items with the information needed to render pagination.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items of the current page.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The page size.</param>
/// <param name="TotalCount">The total number of items matching the query.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>
    /// Gets the total number of pages (at least one).
    /// </summary>
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Gets the 1-based index of the first item on the page (0 when empty).
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>
    /// Gets the 1-based index of the last item on the page.
    /// </summary>
    public int LastItemIndex => Math.Min(TotalCount, Page * PageSize);

    /// <summary>
    /// Creates an empty page.
    /// </summary>
    /// <param name="request">The original request.</param>
    /// <returns>The empty page.</returns>
    public static PagedResult<T> Empty(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new([], 1, request.PageSize, 0);
    }

    /// <summary>
    /// Projects the items to another type keeping the paging information.
    /// </summary>
    /// <typeparam name="TResult">The target item type.</typeparam>
    /// <param name="selector">The projection.</param>
    /// <returns>The projected page.</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector) => new([.. Items.Select(selector)], Page, PageSize, TotalCount);
}
