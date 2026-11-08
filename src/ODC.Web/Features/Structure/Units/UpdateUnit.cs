using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Replaces a unit: renames, retypes, moves (never under its own descendants) or deactivates it, within the
/// organizations the current user manages.
/// </summary>
public static class UpdateUnit
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The unit.</param>
    /// <param name="Unit">The new data.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Command(Guid Id, SaveUnitCommand Unit) : ICommand<Result>;

    /// <summary>
    /// Validates, checks the scope and saves the unit.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, IAccessScope accessScope, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var (id, data) = (request.Id, request.Unit);
            var unit = await db.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (unit is null)
            {
                return UnitTree.NotFound();
            }

            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
            if (!scope.Contains(id))
            {
                return Error.Forbidden(Texts.T(UnitTree.OutsideScopeMessage));
            }

            if (await UnitTree.ValidateAsync(db, id, data, cancellationToken) is { } invalid)
            {
                return invalid;
            }

            if (data.ParentId != unit.ParentId)
            {
                if (data.ParentId is not { } parentId)
                {
                    if (!scope.IsCabinetWide)
                    {
                        return Error.Forbidden(Texts.T(UnitTree.TopLevelMessage));
                    }
                }
                else if (!scope.Contains(parentId))
                {
                    return Error.Forbidden(Texts.T(UnitTree.OutsideScopeMessage));
                }
            }

            // Turning an organization into an ordinary unit merges it into the organization above and widens the scope
            // of its staff.
            if (!scope.IsCabinetWide
                && await db.UnitTypes.AnyAsync(t => t.Id == unit.TypeId && t.IsOrganization, cancellationToken)
                && !await db.UnitTypes.AnyAsync(t => t.Id == data.TypeId && t.IsOrganization, cancellationToken))
            {
                return Error.Forbidden(Texts.T("Перетворити організацію на звичайний підрозділ може лише адміністратор усіх організацій: це змінює межі організацій і права їхніх працівників."));
            }

            var links = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.ParentId }).ToListAsync(cancellationToken);
            var descendants = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), [id]);
            descendants.Remove(id);

            return await DomainErrors.GuardAsync(async () =>
            {
                unit.Update(data.Code, data.Name, data.ShortName, data.TypeId, data.SortOrder);
                unit.MoveTo(data.ParentId, descendants);
                unit.SetActive(data.IsActive);
                audit.Add(AuditActions.UnitChanged, nameof(OrganizationUnit), unit.Id.ToString(), $"Змінено {unit.Code} «{unit.Name}»");
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/organization-units/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapPut("{id:guid}", HandleAsync).RequirePermission(Permissions.Structure.Manage);

        /// <summary>
        /// Replaces a unit.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            SaveUnitCommand unit,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, unit), cancellationToken)).ToNoContent();
    }
}
