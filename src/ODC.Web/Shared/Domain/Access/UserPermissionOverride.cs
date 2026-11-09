namespace ODC.Web.Shared.Domain.Access;

/// <summary>
/// An individual permission adjustment for one person in one organization, applied on top of the roles of the
/// person's positions there: of type <see cref="PermissionClaimTypes.Permission"/> (granted) or
/// <see cref="PermissionClaimTypes.DeniedPermission"/> (denied), whose value is the permission code.
/// </summary>
public sealed class UserPermissionOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissionOverride"/> class.
    /// </summary>
    /// <param name="userId">The person (member) identifier.</param>
    /// <param name="permission">The permission code.</param>
    /// <param name="isGranted"><see langword="true"/> to grant the permission, <see langword="false"/> to deny it.</param>
    public UserPermissionOverride(Guid userId, string permission, bool isGranted)
    {
        UserId = userId;
        ClaimType = isGranted ? PermissionClaimTypes.Permission : PermissionClaimTypes.DeniedPermission;
        ClaimValue = permission;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissionOverride"/> class for EF Core materialization.
    /// </summary>
    private UserPermissionOverride()
    {
        ClaimType = string.Empty;
        ClaimValue = string.Empty;
    }

    /// <summary>
    /// Gets the identifier (UUID v7).
    /// </summary>
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the person (member) identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the kind: <see cref="PermissionClaimTypes.Permission"/> or <see cref="PermissionClaimTypes.DeniedPermission"/>.
    /// </summary>
    public string ClaimType { get; private set; }

    /// <summary>
    /// Gets the permission code.
    /// </summary>
    public string ClaimValue { get; private set; }
}
