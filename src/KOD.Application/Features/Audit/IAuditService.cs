using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Audit;

/// <summary>
/// Read access to the audit trail.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Lists audit entries, newest first.
    /// </summary>
    /// <param name="query">The paging and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of entries.</returns>
    Task<Result<PagedResult<AuditEntryDto>>> ListAsync(AuditQuery query, CancellationToken cancellationToken);
}
