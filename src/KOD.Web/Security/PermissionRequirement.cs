using Microsoft.AspNetCore.Authorization;

namespace KOD.Web.Security;

/// <summary>
/// Requires at least one of the listed permissions.
/// </summary>
/// <param name="permissions">The accepted permission codes.</param>
public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the accepted permission codes.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; } = permissions;
}
