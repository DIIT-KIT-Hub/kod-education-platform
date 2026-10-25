using Microsoft.AspNetCore.Authorization;

namespace KOD.Web.Security;

/// <summary>
/// Restricts a page, controller or action to users that have at least one of the permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permissions">The accepted permission codes.</param>
    public RequirePermissionAttribute(params string[] permissions)
        : base(PermissionPolicyProvider.Prefix + string.Join('|', permissions)) => Permissions = permissions;

    /// <summary>
    /// Gets the accepted permission codes.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }
}
