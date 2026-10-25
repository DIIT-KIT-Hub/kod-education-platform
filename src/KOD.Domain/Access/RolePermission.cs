namespace KOD.Domain.Access;

/// <summary>
/// A permission granted by a <see cref="Role"/>.
/// </summary>
public sealed class RolePermission
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermission"/> class.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="permission">The permission code.</param>
    public RolePermission(Guid roleId, string permission)
    {
        RoleId = roleId;
        Permission = permission;
    }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the permission code.
    /// </summary>
    public string Permission { get; private set; }
}
