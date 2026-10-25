using KOD.Application.Common.Paging;
using KOD.Application.Features.Roles;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The role grid.
/// </summary>
public partial class RolesIndex
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
    /// Gets or sets the role service.
    /// </summary>
    [Inject]
    private IRoleService Roles { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<RoleListItem>? Result { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var result = await Roles.ListAsync(
            new PageRequest { Search = Search, Page = Page ?? 1, PageSize = PageSize ?? PageRequest.DefaultPageSize, SortBy = SortBy, Descending = Descending },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<RoleListItem>.Empty(new PageRequest());
    }
}
