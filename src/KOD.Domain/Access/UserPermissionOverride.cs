namespace KOD.Domain.Access;

/// <summary>
/// An individual permission adjustment for a single user that is applied on top of the user's roles.
/// </summary>
public sealed class UserPermissionOverride
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
        Permission = permission;
        IsGranted = isGranted;
    }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the permission code.
    /// </summary>
    public string Permission { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the permission is granted (<see langword="true"/>) or explicitly denied.
    /// </summary>
    public bool IsGranted { get; private set; }
}
