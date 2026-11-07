using Microsoft.AspNetCore.Mvc;
using ODC.Application.Common.Paging;
using ODC.Application.Features.Audit;
using ODC.Domain.Access;
using ODC.Web.Security;

namespace ODC.Web.Api;

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
