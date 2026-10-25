using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Structure;

/// <summary>
/// The organizational tree: every organization with its units.
/// </summary>
public partial class StructureIndex
{
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
    /// Gets or sets the root nodes.
    /// </summary>
    private IReadOnlyList<UnitTreeNode> Tree { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the user manages the structure.
    /// </summary>
    private bool CanManage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user may create top-level organizations.
    /// </summary>
    private bool CanCreateOrganization { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState).User;
        CanManage = user.HasPermission(Permissions.Structure.Manage);
        CanCreateOrganization = CanManage && user.HasPermission(Permissions.Administration.AllOrganizations);
        var result = await Organization.GetTreeAsync(includeInactive: false, CancellationToken.None);
        Tree = result.IsSuccess ? result.Value : [];
    }
}
