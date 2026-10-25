using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The unit type catalog.
/// </summary>
public partial class UnitTypesIndex
{
    /// <summary>
    /// Gets or sets the unit type service.
    /// </summary>
    [Inject]
    private IUnitTypeService UnitTypes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the current user manages the structure of every organization
    /// (top-level organizations and the unit type catalog shared by all of them).
    /// </summary>
    private bool CabinetWide { get; set; }

    /// <summary>
    /// Gets or sets the loaded types.
    /// </summary>
    private IReadOnlyList<UnitTypeItem>? Items { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        CabinetWide = (await AuthenticationState).User.HasPermission(Permissions.Administration.AllOrganizations);
        var result = await UnitTypes.ListAsync(CancellationToken.None);
        Items = result.IsSuccess ? result.Value : [];
    }
}
