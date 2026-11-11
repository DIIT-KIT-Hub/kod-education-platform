using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Organizations;

/// <summary>
/// Lists the registry of organizations: state, database, schema and people.
/// </summary>
public static class ListOrganizations
{
    /// <summary>
    /// The request.
    /// </summary>
    [RequirePermission(Permissions.Administration.AllOrganizations)]
    public sealed record Query : IQuery<Result<List<OrganizationListItem>>>;

    /// <summary>
    /// Reads the registry (up to a hundred organizations: no paging).
    /// </summary>
    /// <param name="db">The master database.</param>
    /// <param name="schema">The schema the databases must have.</param>
    internal sealed class Handler(MasterDbContext db, OrganizationSchema schema) : IRequestHandler<Query, Result<List<OrganizationListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<List<OrganizationListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var organizations = await db.Organizations.AsNoTracking().OrderBy(static o => o.CreatedAt).ThenBy(static o => o.Code).ToListAsync(cancellationToken);
            var members = await db.Memberships.AsNoTracking()
                .GroupBy(static m => m.OrganizationId)
                .Select(static g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);
            return organizations
                .Select(o => new OrganizationListItem(
                    o.Id,
                    o.Code,
                    o.Name,
                    o.ShortName,
                    o.State,
                    o.DatabaseName,
                    o.SchemaVersion,
                    schema.IsCurrent(o),
                    o.MigratedAt,
                    o.CreatedAt,
                    members.GetValueOrDefault(o.Id),
                    o.OwnerPassword is not null))
                .ToList();
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organizations</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapOrganizations().MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists the organizations.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organizations.</returns>
        private static async Task<Results<Ok<List<OrganizationListItem>>, ProblemHttpResult>> HandleAsync(
            IRequestHandler<Query, Result<List<OrganizationListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(), cancellationToken)).ToOk();
    }
}
