using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Audit;

/// <summary>
/// The audit trail (read-only), newest first.
/// </summary>
public static class ListAuditEntries
{
    /// <summary>
    /// Paging and filter parameters of the audit trail.
    /// </summary>
    [RequirePermission(Permissions.Audit.View)]
    public sealed record Query : PageRequest, IQuery<Result<PagedResult<AuditEntryDto>>>
    {
        /// <summary>
        /// Gets the action code prefix filter (e.g. <c>auth.</c>).
        /// </summary>
        public string? Action { get; init; }

        /// <summary>
        /// Gets the start of the period (inclusive).
        /// </summary>
        public DateOnly? From { get; init; }

        /// <summary>
        /// Gets the end of the period (inclusive).
        /// </summary>
        public DateOnly? To { get; init; }
    }

    /// <summary>
    /// Filters and pages the entries.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Query, Result<PagedResult<AuditEntryDto>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<AuditEntryDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var entries = db.AuditEntries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                var prefix = request.Action.Trim();
                entries = entries.Where(e => e.Action.StartsWith(prefix));
            }

            // The dates are Kyiv calendar days (as shown in the log), not UTC days.
            if (request.From is { } from)
            {
                var start = KyivTime.StartOfDay(from);
                entries = entries.Where(e => e.OccurredAt >= start);
            }

            if (request.To is { } to)
            {
                var end = KyivTime.StartOfDay(to.AddDays(1));
                entries = entries.Where(e => e.OccurredAt < end);
            }

            if (request.Search is { } search)
            {
                var term = search;
                entries = entries.Where(e =>
                    (e.UserName != null && e.UserName.Contains(term))
                    || (e.Details != null && e.Details.Contains(term))
                    || (e.IpAddress != null && e.IpAddress.Contains(term)));
            }

            return await entries
                .OrderByDirection(static e => e.OccurredAt, request.SortBy != "occurred" || request.IsDescending)
                .Select(e => new AuditEntryDto(
                    e.Id,
                    e.OccurredAt,
                    e.Action,
                    e.UserId,
                    e.UserName,
                    e.EntityType,
                    e.EntityId,
                    e.Details,
                    e.IpAddress,
                    db.Users.Where(u => u.Id == e.UserId).Select(static u => u.PhotoId).FirstOrDefault()))
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/audit-entries</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => api.MapGet("audit-entries", HandleAsync).RequirePermission(Permissions.Audit.View).WithTags("AuditEntries");

        /// <summary>
        /// Lists audit entries, newest first.
        /// </summary>
        /// <param name="query">Paging and filters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A page of entries.</returns>
        private static async Task<Results<Ok<PagedResult<AuditEntryDto>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, Result<PagedResult<AuditEntryDto>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }
}
