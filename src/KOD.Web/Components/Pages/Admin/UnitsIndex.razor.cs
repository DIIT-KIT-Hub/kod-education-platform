using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
using KOD.Domain.Organization;
using KOD.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The unit management grid.
/// </summary>
public partial class UnitsIndex
{
    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Search { get; set; }

    /// <summary>
    /// Gets or sets the type filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Type { get; set; }

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
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<UnitListItem>? Result { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var result = await Organization.ListAsync(
            new UnitQuery
            {
                Search = Search,
                Type = QueryValues.Enum<OrganizationUnitType>(Type),
                Page = Page ?? 1,
                PageSize = PageSize ?? PageRequest.DefaultPageSize,
                SortBy = SortBy,
                Descending = Descending,
            },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<UnitListItem>.Empty(new PageRequest());
    }
}
