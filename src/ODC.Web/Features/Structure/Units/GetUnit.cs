using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Gets a unit with its path, organization, subtree and whether the current user may edit it.
/// </summary>
public static class GetUnit
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The unit.</param>
    [RequirePermission(Permissions.Structure.View)]
    public sealed record Query(Guid Id) : IQuery<Result<UnitDetails>>;

    /// <summary>
    /// Reads the unit.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="accessScope">The administration scopes.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, IAccessScope accessScope) : IRequestHandler<Query, Result<UnitDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<UnitDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            var units = await UnitTree.LoadAsync(db, cancellationToken);
            var unit = units.Find(u => u.Id == id);
            if (unit is null)
            {
                return UnitTree.NotFound();
            }

            var byId = units.ToDictionary(static u => u.Id);
            var parents = units.ToDictionary(static u => u.Id, static u => u.ParentId);
            var organizations = units.Where(static u => u.Type.IsOrganization).Select(static u => u.Id).ToHashSet();
            var path = OrganizationTree.PathTo(parents, id)
                .Select(pid => new UnitRef(pid, byId[pid].Name))
                .ToList();
            var organization = byId[OrganizationTree.OrganizationOf(parents, organizations, id)];
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);

            return new UnitDetails(
                unit.Id,
                unit.Code,
                unit.Name,
                unit.ShortName,
                unit.Type,
                unit.IsActive,
                unit.ParentId,
                unit.SortOrder,
                path,
                new UnitRef(organization.Id, organization.Name),
                UnitTree.Build(units, id, includeInactive: true),
                unit.StaffCount,
                currentUser.Has(Permissions.Structure.Manage) && scope.Contains(id));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet("{id:guid}", HandleAsync);

        /// <summary>
        /// Gets a unit.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The unit.</returns>
        private static async Task<Results<Ok<UnitDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<UnitDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
