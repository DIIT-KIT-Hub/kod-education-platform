using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Access;

/// <summary>
/// An individual permission adjustment for a single user that is applied on top of the user's roles: an Identity user
/// claim (table <c>user_claims</c>) of type <see cref="PermissionClaimTypes.Permission"/> (granted) or
/// <see cref="PermissionClaimTypes.DeniedPermission"/> (denied) whose value is the permission code.
/// </summary>
public sealed class UserPermissionOverride : IdentityUserClaim<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissionOverride"/> class.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="permission">The permission code.</param>
    /// <param name="isGranted"><see langword="true"/> to grant the permission, <see langword="false"/> to deny it.</param>
    public UserPermissionOverride(Guid userId, string permission, bool isGranted)
    {
        UserId = userId;
        ClaimType = isGranted ? PermissionClaimTypes.Permission : PermissionClaimTypes.DeniedPermission;
        ClaimValue = permission;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissionOverride"/> class for EF Core and the Identity user store
    /// (<see cref="UserManager{TUser}.AddClaimAsync"/>), which fill the properties afterwards.
    /// </summary>
    public UserPermissionOverride()
    {
    }
}
