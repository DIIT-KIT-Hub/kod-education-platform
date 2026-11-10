using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Deletes a custom role that nobody holds.
/// </summary>
public static class DeleteRole
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The role.</param>
    [RequirePermission(Permissions.Roles.Manage)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Deletes the role when allowed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="across">Reads every organization (positions with the role).</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(MasterDbContext db, IAcrossOrganizations across, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (role is null)
            {
                return RoleRules.NotFound();
            }

            if (role.IsSystem)
            {
                return Error.Validation(Texts.T("Системну роль не можна видалити."));
            }

            if ((await across.EveryAsync(organization => organization.StaffPositions.AnyAsync(p => p.RoleId == request.Id, cancellationToken), cancellationToken)).Exists(static p => p.Value))
            {
                return Error.Conflict(Texts.T("Роль призначена користувачам — спочатку змініть їхні посади."));
            }

            db.Roles.Remove(role);
            audit.Add(AuditActions.RoleDeleted, nameof(Role), role.Id.ToString(), $"Видалено роль «{role.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/roles/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapRoles().MapDelete("{id:guid}", HandleAsync);

        /// <summary>
        /// Deletes a custom role that nobody holds.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
