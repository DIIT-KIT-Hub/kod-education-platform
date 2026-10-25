using KOD.Application.Common.Paging;
using KOD.Application.Features.News;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.News;

/// <summary>
/// The news management grid (drafts included).
/// </summary>
public partial class NewsManage
{
    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Search { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the sort key.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is descending.
    /// </summary>
    [SupplyParameterFromQuery]
    private bool Descending { get; set; }

    /// <summary>
    /// Gets or sets the news service.
    /// </summary>
    [Inject]
    private INewsService News { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<NewsListItem>? Result { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var result = await News.ListAsync(
            new NewsQuery
            {
                IncludeDrafts = true,
                Search = Search,
                Page = Page ?? 1,
                PageSize = PageSize ?? PageRequest.DefaultPageSize,
                SortBy = SortBy ?? "updated",
                Descending = SortBy is null || Descending,
            },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<NewsListItem>.Empty(new PageRequest());
    }
}
