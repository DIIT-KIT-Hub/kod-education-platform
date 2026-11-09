using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Deletes a unit type that no unit uses.
/// </summary>
public static class DeleteUnitType
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The type.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Deletes the type when unused.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="organization">The organization's database (units of the type).</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(MasterDbContext db, OrganizationDbContext organization, ICurrentUser currentUser, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (UnitTypeCatalog.CheckEditor(currentUser) is { } denied)
            {
                return denied;
            }

            var type = await db.UnitTypes.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (type is null)
            {
                return UnitTypeCatalog.NotFound();
            }

            var used = await organization.OrganizationUnits.CountAsync(u => u.TypeId == request.Id, cancellationToken);
            if (used > 0)
            {
                return Error.Conflict(Texts.T("Тип «{0}» мають підрозділи ({1}). Спершу змініть їхній тип.", Texts.T(type.Name), used));
            }

            db.UnitTypes.Remove(type);
            audit.Add(AuditActions.UnitTypeDeleted, nameof(UnitType), type.Id.ToString(), $"Видалено тип {type.Code} «{type.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/unit-types/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnitTypes().MapDelete("{id:guid}", HandleAsync);

        /// <summary>
        /// Deletes a unit type that no unit uses.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
