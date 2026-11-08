using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Replaces a role's name, description and permissions. Holders get the new permissions with their next access token
/// (at most its lifetime later).
/// </summary>
public static class UpdateRole
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The role.</param>
    /// <param name="Role">The new data.</param>
    [RequirePermission(Permissions.Roles.Manage)]
    public sealed record Command(Guid Id, SaveRoleCommand Role) : ICommand<Result>;

    /// <summary>
    /// Validates the name and saves the role.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var data = request.Role;
            var role = await db.Roles.Include(static r => r.Permissions).FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (role is null)
            {
                return RoleRules.NotFound();
            }

            if (await RoleRules.NameTakenAsync(db, data.Name, request.Id, cancellationToken))
            {
                return RoleRules.NameTaken(data.Name);
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                role.Update(data.Name, data.Description, data.IsLeadership);
                role.SetPermissions(data.Permissions);
                audit.Add(AuditActions.RoleChanged, nameof(Role), role.Id.ToString(), $"Змінено роль «{role.Name}»: {string.Join(", ", role.Permissions.Select(static p => p.ClaimValue))}");
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/roles/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapRoles().MapPut("{id:guid}", HandleAsync);

        /// <summary>
        /// Replaces a role's name, description and permissions.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="role">The role.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            SaveRoleCommand role,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, role), cancellationToken)).ToNoContent();
    }
}
