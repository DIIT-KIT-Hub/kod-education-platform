using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Shared.Access;

/// <summary>
/// A role of the master database's catalog, as the organizations' side needs it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The stable code.</param>
/// <param name="Name">The stored (untranslated) name.</param>
/// <param name="IsSystem">Whether it is a predefined role.</param>
/// <param name="IsLeadership">Whether it is a leadership role.</param>
/// <param name="Permissions">The permissions it grants.</param>
internal sealed record CatalogRole(Guid Id, string Code, string Name, bool IsSystem, bool IsLeadership, IReadOnlySet<string> Permissions)
{
    /// <summary>
    /// Gets a value indicating whether it is the system administrator's role (every permission).
    /// </summary>
    public bool IsAdministrator => Code == SystemRoles.Administrator;

    /// <summary>
    /// Whether the role grants a permission.
    /// </summary>
    /// <param name="permission">The permission code.</param>
    /// <returns>Whether it does.</returns>
    public bool Grants(string permission) => IsAdministrator || Permissions.Contains(permission);
}
