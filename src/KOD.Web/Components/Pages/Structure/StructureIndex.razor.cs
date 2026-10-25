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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        CanManage = (await AuthenticationState).User.HasPermission(Permissions.Structure.Manage);
        var result = await Organization.GetTreeAsync(includeInactive: false, CancellationToken.None);
        Tree = result.IsSuccess ? result.Value : [];
    }
}
