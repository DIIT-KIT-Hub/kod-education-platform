using Microsoft.AspNetCore.Authorization;

namespace KOD.Web.Security;

/// <summary>
/// Succeeds when the user's permission claims contain any permission of the requirement.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.Permissions.Any(context.User.HasPermission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
