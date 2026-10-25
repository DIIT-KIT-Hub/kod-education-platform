using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KOD.DesignSystem.Components;

/// <summary>
/// Query-string based pagination with a page size selector.
/// </summary>
public partial class Pagination
{
    /// <summary>
    /// Page size options.
    /// </summary>
    private static readonly int[] DefaultPageSizes = [10, 20, 50, 100];

    /// <summary>
    /// Formatting culture of the counters.
    /// </summary>
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>
    /// Gets or sets the 1-based page number.
    /// </summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    [Parameter]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page size options.
    /// </summary>
    [Parameter]
    public IReadOnlyList<int> PageSizes { get; set; } = DefaultPageSizes;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));

    /// <summary>
    /// Gets the 1-based index of the first item on the page.
    /// </summary>
    private int FirstItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>
    /// Gets the 1-based index of the last item on the page.
    /// </summary>
    private int LastItem => Math.Min(TotalCount, Page * PageSize);

    /// <summary>
    /// Gets a unique id for the page size select.
    /// </summary>
    private string SizeId { get; } = $"page-size-{Guid.NewGuid():N}"[..20];

    /// <summary>
    /// Gets the current path without the query string.
    /// </summary>
    private string CurrentPath => new Uri(Navigation.Uri).GetLeftPart(UriPartial.Path);

    /// <summary>
    /// Gets the current query parameters except paging, preserved by the page size form.
    /// </summary>
    private IEnumerable<(string Key, string Value)> PreservedQuery =>
        QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query)
            .Where(static p => p.Key is not ("page" or "pageSize"))
            .SelectMany(static p => p.Value.Select(v => (p.Key, v ?? string.Empty)));

    /// <summary>
    /// Gets the page numbers to show; <see langword="null"/> marks a gap.
    /// </summary>
    private IEnumerable<int?> Window
    {
        get
        {
            var pages = new SortedSet<int> { 1, TotalPages };
            for (var p = Page - 2; p <= Page + 2; p++)
            {
                if (p >= 1 && p <= TotalPages)
                {
                    pages.Add(p);
                }
            }

            var previous = 0;
            foreach (var page in pages)
            {
                if (page - previous > 1)
                {
                    yield return null;
                }

                yield return page;
                previous = page;
            }
        }
    }

    /// <summary>
    /// Builds the link to a page.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The URL.</returns>
    private string PageHref(int page)
        => Navigation.GetUriWithQueryParameter("page", page <= 1 ? null : Math.Min(page, TotalPages));
}
