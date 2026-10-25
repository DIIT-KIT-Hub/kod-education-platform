using KOD.Application.Common.Paging;
using KOD.Application.Features.Audit;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The audit trail (read-only).
/// </summary>
/// <param name="audit">The audit service.</param>
[Route("api/v1/audit-entries")]
[RequirePermission(Permissions.Audit.View)]
public sealed class AuditEntriesController(IAuditService audit) : ApiControllerBase
{
    /// <summary>
    /// Lists audit entries, newest first.
    /// </summary>
    /// <param name="query">Paging and filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of entries.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> List([FromQuery] AuditQuery query, CancellationToken cancellationToken)
        => FromResult(await audit.ListAsync(query, cancellationToken));
}
