using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// The active units where an administrative permission of the current user applies, as a flat hierarchical list for
/// select inputs (see <see cref="IAccessScope.GetAdministrationScopeAsync"/>). Other features send this query with their
/// own permission (users, news); the structure endpoint asks with <c>structure.manage</c>.
/// </summary>
public static class ListUnitOptions
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Permission">The administrative permission, e.g. <c>users.manage</c>.</param>
    public sealed record Query(string Permission) : IQuery<IReadOnlyList<UnitOption>>;

    /// <summary>
    /// Walks the tree and keeps the units of the scope.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="catalog">The shared catalogs (unit types).</param>
    /// <param name="accessScope">The administration scopes.</param>
    internal sealed class Handler(OrganizationDbContext db, Catalog catalog, IAccessScope accessScope) : IRequestHandler<Query, IReadOnlyList<UnitOption>>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<UnitOption>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var scope = await accessScope.GetAdministrationScopeAsync(request.Permission, cancellationToken);
            if (scope.Units is { Count: 0 })
            {
                return [];
            }

            var units = await UnitTree.LoadAsync(db, catalog, cancellationToken);
            var result = new List<UnitOption>(units.Count);
            void Walk(IReadOnlyList<UnitTreeNode> nodes, int depth)
            {
                foreach (var node in nodes)
                {
                    // Units outside the scope are skipped without indenting their descendants, so the scope's topmost
                    // units have depth 0.
                    var included = scope.Contains(node.Id);
                    if (included)
                    {
                        result.Add(new UnitOption(node.Id, node.Name, node.Code, node.Type.IsOrganization, depth));
                    }

                    Walk(node.Children, included ? depth + 1 : depth);
                }
            }

            Walk(UnitTree.Build(units, null, includeInactive: false), 0);
            return result;
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units/options</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet("options", HandleAsync).RequirePermission(Permissions.Structure.Manage);

        /// <summary>
        /// Lists the units whose structure the user manages, in tree order (parents for new units, organization filters).
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The units.</returns>
        private static async Task<Ok<IReadOnlyList<UnitOption>>> HandleAsync(IRequestHandler<Query, IReadOnlyList<UnitOption>> handler, CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(new Query(Permissions.Structure.Manage), cancellationToken));
    }
}
