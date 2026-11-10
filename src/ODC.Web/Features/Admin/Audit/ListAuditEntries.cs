using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
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
    /// Filters and pages the entries of every trail: the platform's (master database) and each organization's (its
    /// database), merged by time (<c>audit.view</c> is platform-wide).
    /// </summary>
    /// <param name="db">The master database.</param>
    /// <param name="across">Reads every organization.</param>
    internal sealed class Handler(MasterDbContext db, IAcrossOrganizations across) : IRequestHandler<Query, Result<PagedResult<AuditEntryDto>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<AuditEntryDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var descending = request.SortBy != "occurred" || request.IsDescending;
            var size = request.CurrentPageSize;
            var needed = request.CurrentPage * size;
            var platform = Filter(db.AuditEntries.AsNoTracking(), request);
            var total = await platform.CountAsync(cancellationToken);

            // Each trail gives at most the entries up to the end of the page; the merge takes the page from them.
            var candidates = (await platform.OrderByDirection(static e => e.OccurredAt, descending).Take(needed).ToListAsync(cancellationToken))
                .ConvertAll(static e => (Entry: e, Code: (string?)null));
            var organizations = await across.EveryAsync(
                async organization =>
                {
                    var entries = Filter(organization.AuditEntries.AsNoTracking(), request);
                    return (Count: await entries.CountAsync(cancellationToken), Top: await entries.OrderByDirection(static e => e.OccurredAt, descending).Take(needed).ToListAsync(cancellationToken));
                },
                cancellationToken);
            foreach (var part in organizations)
            {
                total += part.Value.Count;
                candidates.AddRange(part.Value.Top.Select(e => (Entry: e, Code: (string?)part.Organization.Code)));
            }

            var page = (descending ? candidates.OrderByDescending(static e => e.Entry.OccurredAt) : candidates.OrderBy(static e => e.Entry.OccurredAt))
                .ThenBy(static e => e.Entry.Id)
                .Skip((request.CurrentPage - 1) * size)
                .Take(size)
                .ToList();
            var userIds = page.Select(static e => e.Entry.UserId).OfType<Guid>().Distinct().ToList();
            var photos = await db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(static u => new { u.Id, u.PhotoId })
                .ToDictionaryAsync(static u => u.Id, static u => u.PhotoId, cancellationToken);

            return new PagedResult<AuditEntryDto>(
                page.ConvertAll(p => new AuditEntryDto(
                    p.Entry.Id,
                    p.Entry.OccurredAt,
                    p.Entry.Action,
                    p.Entry.UserId,
                    p.Entry.UserName,
                    p.Entry.EntityType,
                    p.Entry.EntityId,
                    p.Entry.Details,
                    p.Entry.IpAddress,
                    p.Entry.UserId is { } user ? photos.GetValueOrDefault(user) : null,
                    p.Code)),
                request.CurrentPage,
                size,
                total);
        }

        /// <summary>
        /// Applies the filters of the log to one trail.
        /// </summary>
        /// <param name="entries">The trail.</param>
        /// <param name="request">The filters.</param>
        /// <returns>The filtered trail.</returns>
        private static IQueryable<AuditEntry> Filter(IQueryable<AuditEntry> entries, Query request)
        {
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

            return entries;
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
