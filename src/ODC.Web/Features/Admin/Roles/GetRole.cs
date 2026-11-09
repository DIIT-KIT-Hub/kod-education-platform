using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Gets a role for its editor (name and description as stored, not translated).
/// </summary>
public static class GetRole
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The role.</param>
    [RequirePermission(Permissions.Roles.Manage)]
    public sealed record Query(Guid Id) : IQuery<Result<RoleDetails>>;

    /// <summary>
    /// Reads the role.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="organization">The organization's database (positions with the role).</param>
    internal sealed class Handler(MasterDbContext db, OrganizationDbContext organization) : IRequestHandler<Query, Result<RoleDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<RoleDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var role = await db.Roles.AsNoTracking().Include(static r => r.Permissions).FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (role is null)
            {
                return RoleRules.NotFound();
            }

            var users = await organization.StaffPositions.Where(p => p.RoleId == request.Id).Select(static p => p.UserId).Distinct().CountAsync(cancellationToken);
            return new RoleDetails(
                role.Id,
                role.Code,
                role.Name,
                role.Description,
                role.IsSystem,
                role.IsAdministrator,
                role.IsLeadership,
                [.. role.Permissions.Select(static p => p.ClaimValue!)],
                users);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/roles/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapRoles().MapGet("{id:guid}", HandleAsync);

        /// <summary>
        /// Gets a role.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The role.</returns>
        private static async Task<Results<Ok<RoleDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<RoleDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
