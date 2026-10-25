using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Web.DesignSystem.Components;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Structure;

/// <summary>
/// A unit page: nested units and paginated staff.
/// </summary>
public partial class UnitPage
{
    /// <summary>
    /// Gets or sets the unit identifier.
    /// </summary>
    [Parameter]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the staff search term.
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
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unit.
    /// </summary>
    private UnitDetails? Unit { get; set; }

    /// <summary>
    /// Gets or sets the staff page.
    /// </summary>
    private PagedResult<UnitStaffItem>? Staff { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user manages the structure.
    /// </summary>
    private bool CanManage { get; set; }

    /// <summary>
    /// Gets the breadcrumb trail.
    /// </summary>
    private IReadOnlyList<BreadcrumbItem> Breadcrumbs =>
    [
        new("Структура", "/structure"),
        .. (Unit?.Path ?? []).Select(p => new BreadcrumbItem(p.Name, p.Id == Id ? null : $"/structure/{p.Id}")),
    ];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        CanManage = (await AuthenticationState).User.HasPermission(Permissions.Structure.Manage);
        var unit = await Organization.GetAsync(Id, CancellationToken.None);
        var staff = unit.IsSuccess
            ? await Organization.GetStaffAsync(
                Id,
                new PageRequest { Search = Search, Page = Page ?? 1, PageSize = PageSize ?? PageRequest.DefaultPageSize },
                CancellationToken.None)
            : null;

        Unit = unit.IsSuccess ? unit.Value : null;
        Staff = staff is { IsSuccess: true } ? staff.Value : PagedResult<UnitStaffItem>.Empty(new PageRequest());
    }
}
