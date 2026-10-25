using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// User exports to XLSX/CSV files.
/// </summary>
/// <param name="users">The user service.</param>
[Route("api/v1/user-exports")]
[RequirePermission(Permissions.Users.Export)]
public sealed class UserExportsController(IUserService users) : ApiControllerBase
{
    /// <summary>
    /// Exports users matching the filters (one row per position; the format is compatible with imports).
    /// </summary>
    /// <param name="query">The filters (paging is ignored).</param>
    /// <param name="format">The file format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    [HttpGet]
    [Produces("application/octet-stream")]
    public async Task<ActionResult> Get([FromQuery] UserQuery query, [FromQuery] SpreadsheetFormat format, CancellationToken cancellationToken)
        => FromFile(await users.ExportAsync(query, format, cancellationToken));
}
