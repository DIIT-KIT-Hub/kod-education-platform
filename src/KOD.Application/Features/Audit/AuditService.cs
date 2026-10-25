using KOD.Application.Abstractions;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Audit;

/// <summary>
/// Default <see cref="IAuditService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
internal sealed class AuditService(IApplicationDbContext db, ICurrentUser currentUser) : IAuditService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditEntryDto>>> ListAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.Audit.View))
        {
            return Error.Forbidden();
        }

        var entries = db.AuditEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var prefix = query.Action.Trim();
            entries = entries.Where(e => e.Action.StartsWith(prefix));
        }

        // The dates are Kyiv calendar days (as shown in the log), not UTC days.
        if (query.From is { } from)
        {
            var start = KyivTime.StartOfDay(from);
            entries = entries.Where(e => e.OccurredAt >= start);
        }

        if (query.To is { } to)
        {
            var end = KyivTime.StartOfDay(to.AddDays(1));
            entries = entries.Where(e => e.OccurredAt < end);
        }

        if (query.Search is { } search)
        {
            var term = search;
            entries = entries.Where(e =>
                (e.UserName != null && e.UserName.Contains(term))
                || (e.Details != null && e.Details.Contains(term))
                || (e.IpAddress != null && e.IpAddress.Contains(term)));
        }

        return await entries
            .OrderByDirection(static e => e.OccurredAt, query.SortBy != "occurred" || query.Descending)
            .Select(static e => new AuditEntryDto(e.Id, e.OccurredAt, e.Action, e.UserId, e.UserName, e.EntityType, e.EntityId, e.Details, e.IpAddress))
            .ToPagedResultAsync(query, cancellationToken);
    }
}
