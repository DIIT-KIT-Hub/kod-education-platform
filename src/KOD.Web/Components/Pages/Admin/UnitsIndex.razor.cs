using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
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
    /// Gets or sets the unit type filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? Type { get; set; }

    /// <summary>
    /// Gets or sets the organization filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? Organization { get; set; }

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
    private IOrganizationService OrganizationService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unit type service.
    /// </summary>
    [Inject]
    private IUnitTypeService UnitTypes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unit types for the filter.
    /// </summary>
    private IReadOnlyList<UnitTypeRef> Types { get; set; } = [];

    /// <summary>
    /// Gets or sets the top-level organizations for the filter.
    /// </summary>
    private IReadOnlyList<UnitOption> Organizations { get; set; } = [];

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<UnitListItem>? Result { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        Types = await UnitTypes.GetOptionsAsync(CancellationToken.None);
        Organizations = [.. (await OrganizationService.GetOptionsAsync(CancellationToken.None)).Where(static u => u.Depth == 0)];
        var result = await OrganizationService.ListAsync(
            new UnitQuery
            {
                Search = Search,
                TypeId = Type,
                OrganizationId = Organization,
                Page = Page ?? 1,
                PageSize = PageSize ?? PageRequest.DefaultPageSize,
                SortBy = SortBy,
                Descending = Descending,
            },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<UnitListItem>.Empty(new PageRequest());
    }
}
