using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Access;

/// <summary>
/// A permission granted by a <see cref="Role"/>: an Identity role claim of type <see cref="PermissionClaimTypes.Permission"/>
/// whose value is the permission code (table <c>role_claims</c>).
/// </summary>
public sealed class RolePermission : IdentityRoleClaim<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermission"/> class.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="permission">The permission code.</param>
    public RolePermission(Guid roleId, string permission)
    {
        RoleId = roleId;
        ClaimType = PermissionClaimTypes.Permission;
        ClaimValue = permission;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermission"/> class for EF Core and the Identity role store
    /// (<see cref="RoleManager{TRole}.AddClaimAsync"/>), which fill the properties afterwards.
    /// </summary>
    public RolePermission()
    {
    }
}
