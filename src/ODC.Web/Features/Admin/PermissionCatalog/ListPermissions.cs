using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Features.Admin.PermissionCatalog;

/// <summary>
/// The catalog of permissions and the standard roles (static data: no handler needed).
/// </summary>
public static class ListPermissions
{
    /// <summary>
    /// <c>GET /api/v1/permissions</c> and <c>GET /api/v1/permissions/system-roles</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var permissions = api.MapGroup("permissions").WithTags("Permissions");
            permissions.MapGet(string.Empty, List);
            permissions.MapGet("system-roles", SystemRoleList);
        }

        /// <summary>
        /// Lists every permission with its module and description.
        /// </summary>
        /// <returns>The permissions.</returns>
        private static Ok<IReadOnlyList<PermissionDefinition>> List() => TypedResults.Ok(Permissions.All);

        /// <summary>
        /// Lists the standard roles created at deployment with their default permissions (the manual's reference).
        /// </summary>
        /// <returns>The roles.</returns>
        private static Ok<IReadOnlyList<SystemRoleDefinition>> SystemRoleList() => TypedResults.Ok(SystemRoles.All);
    }
}
