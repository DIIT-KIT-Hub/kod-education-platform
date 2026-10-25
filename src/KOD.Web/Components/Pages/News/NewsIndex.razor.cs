using KOD.Application.Common.Paging;
using KOD.Application.Features.News;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Web.Components.Shared;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.News;

/// <summary>
/// The news feed with categories, search and pagination.
/// </summary>
public partial class NewsIndex
{
    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Category { get; set; }

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
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the news service.
    /// </summary>
    [Inject]
    private INewsService News { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<NewsListItem>? Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user manages news.
    /// </summary>
    private bool CanManage { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        CanManage = (await AuthenticationState).User.HasPermission(Permissions.News.Manage);
        var result = await News.ListAsync(
            new NewsQuery
            {
                Category = QueryValues.Enum<NewsCategory>(Category),
                Search = Search,
                Page = Page ?? 1,
                PageSize = PageSize ?? 9,
            },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<NewsListItem>.Empty(new PageRequest());
    }
}
