using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Lists the units the current user manages: search, type and organization filters, sorting and paging.
/// </summary>
public static class ListUnits
{
    /// <summary>
    /// Paging and filter parameters of the unit list.
    /// </summary>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Query : PageRequest, IQuery<Result<PagedResult<UnitListItem>>>
    {
        /// <summary>
        /// Gets the unit type filter.
        /// </summary>
        public Guid? TypeId { get; init; }

        /// <summary>
        /// Gets the organization filter (the organization and everything below it).
        /// </summary>
        public Guid? OrganizationId { get; init; }
    }

    /// <summary>
    /// Filters, sorts and pages the units within the administration scope.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="accessScope">The administration scopes.</param>
    internal sealed class Handler(ApplicationDbContext db, IAccessScope accessScope) : IRequestHandler<Query, Result<PagedResult<UnitListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<UnitListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
            var units = db.OrganizationUnits.AsNoTracking();
            if (scope.Units is { } scoped)
            {
                var managed = scoped.ToList();
                units = units.Where(u => managed.Contains(u.Id));
            }

            if (request.TypeId is { } typeId)
            {
                units = units.Where(u => u.TypeId == typeId);
            }

            if (request.OrganizationId is { } organizationId)
            {
                var links = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.ParentId }).ToListAsync(cancellationToken);
                var subtree = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), [organizationId]).ToList();
                units = units.Where(u => subtree.Contains(u.Id));
            }

            if (request.Search is { } search)
            {
                var term = search;
                units = units.Where(u => u.Name.Contains(term) || u.Code.Contains(term));
            }

            units = request.SortBy switch
            {
                "code" => units.OrderByDirection(static u => u.Code, request.IsDescending),
                "type" => units.OrderByDirection(static u => u.Type!.Name, request.IsDescending),
                _ => units.OrderByDirection(static u => u.Name, request.IsDescending),
            };

            return await units
                .Select(u => new UnitListItem(
                    u.Id,
                    u.Code,
                    u.Name,
                    new UnitTypeRef(u.TypeId, StoredText.Display(u.Type!.Name), u.Type.Icon, u.Type.IsOrganization),
                    u.Parent != null ? u.Parent.Name : null,
                    u.IsActive,
                    db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id)))
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units/list</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet("list", HandleAsync).RequirePermission(Permissions.Structure.Manage);

        /// <summary>
        /// Lists the units for management: search, type and organization filters, sorting and paging.
        /// </summary>
        /// <param name="query">The filters and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<UnitListItem>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, Result<PagedResult<UnitListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }
}
