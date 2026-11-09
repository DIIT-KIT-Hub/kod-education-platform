using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Lists the active staff of a unit: leaders first, then by name.
/// </summary>
public static class ListUnitStaff
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="UnitId">The unit.</param>
    /// <param name="Paging">The search and paging.</param>
    [RequirePermission(Permissions.Structure.View)]
    public sealed record Query(Guid UnitId, PageRequest Paging) : IQuery<Result<PagedResult<UnitStaffItem>>>;

    /// <summary>
    /// Reads the page.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="catalog">The shared catalogs (roles).</param>
    internal sealed class Handler(OrganizationDbContext db, Catalog catalog) : IRequestHandler<Query, Result<PagedResult<UnitStaffItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<UnitStaffItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var roles = await catalog.RolesAsync(cancellationToken);
            var leaders = roles.Values.Where(static r => r.IsLeadership).Select(static r => r.Id).ToList();
            var positions = db.StaffPositions.AsNoTracking().Where(p => p.OrganizationUnitId == request.UnitId && p.Member!.IsActive);
            if (request.Paging.Search is { } search)
            {
                var term = search;
                positions = positions.Where(p => p.Member!.LastName.Contains(term) || p.Member.FirstName.Contains(term));
            }

            var page = await positions
                .OrderByDescending(p => leaders.Contains(p.RoleId))
                .ThenBy(static p => p.Member!.LastName)
                .ThenBy(static p => p.Member!.FirstName)
                .Select(static p => new
                {
                    p.UserId,
                    Name = p.Member!.LastName + " " + p.Member.FirstName + (p.Member.MiddleName != null ? " " + p.Member.MiddleName : string.Empty),
                    p.Title,
                    p.RoleId,
                    p.Member.Email,
                    p.Member.PhoneNumber,
                    p.IsPrimary,
                    p.Member.PhotoId,
                })
                .ToPagedResultAsync(request.Paging, cancellationToken);
            return new PagedResult<UnitStaffItem>(
                [
                    .. page.Items.Select(p => new UnitStaffItem(
                        p.UserId,
                        p.Name,
                        StoredText.PositionTitle(p.Title, roles.GetValueOrDefault(p.RoleId)?.Name) ?? string.Empty,
                        p.Email,
                        p.PhoneNumber,
                        p.IsPrimary,
                        roles.GetValueOrDefault(p.RoleId)?.IsLeadership ?? false,
                        p.PhotoId)),
                ],
                page.Page,
                page.PageSize,
                page.TotalCount);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units/{id}/staff</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet("{id:guid}/staff", HandleAsync);

        /// <summary>
        /// Lists the staff of a unit.
        /// </summary>
        /// <param name="id">The unit.</param>
        /// <param name="paging">The search and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<UnitStaffItem>>, ProblemHttpResult>> HandleAsync(
            Guid id,
            [AsParameters] PageRequest paging,
            IRequestHandler<Query, Result<PagedResult<UnitStaffItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id, paging), cancellationToken)).ToOk();
    }
}
