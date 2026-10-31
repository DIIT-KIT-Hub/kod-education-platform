using KOD.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The catalog of permissions.
/// </summary>
[Route("api/v1/permissions")]
public sealed class PermissionsController : ApiControllerBase
{
    /// <summary>
    /// Lists every permission with its module and description.
    /// </summary>
    /// <returns>The permissions.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<PermissionDefinition>> List() => Ok(Permissions.All);

    /// <summary>
    /// Lists the standard roles created at deployment with their default permissions (the manual's reference).
    /// </summary>
    /// <returns>The roles.</returns>
    [HttpGet("system-roles")]
    public ActionResult<IReadOnlyList<SystemRoleDefinition>> SystemRoleList() => Ok(SystemRoles.All);
}
