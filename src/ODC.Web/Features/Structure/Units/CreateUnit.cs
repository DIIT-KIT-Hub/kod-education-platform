using Microsoft.AspNetCore.Http.HttpResults;
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
/// Creates a unit inside an organization the current user manages (top-level organizations: cabinet-wide
/// administrators only).
/// </summary>
public static class CreateUnit
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Unit">The new unit.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Command(SaveUnitCommand Unit) : ICommand<Result<Guid>>;

    /// <summary>
    /// Validates, checks the scope and saves the unit.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, IAccessScope accessScope, IAuditLogger audit) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var data = request.Unit;
            if (await UnitTree.ValidateAsync(db, null, data, cancellationToken) is { } invalid)
            {
                return invalid;
            }

            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
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

            return await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var unit = new OrganizationUnit(data.Code, data.Name, data.TypeId, data.ParentId);
                unit.Update(data.Code, data.Name, data.ShortName, data.TypeId, data.SortOrder);
                unit.SetActive(data.IsActive);
                db.OrganizationUnits.Add(unit);
                audit.Add(AuditActions.UnitChanged, nameof(OrganizationUnit), unit.Id.ToString(), $"Створено {unit.Code} «{unit.Name}»");
                await db.SaveChangesAsync(cancellationToken);
                return unit.Id;
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/organization-units</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapPost(string.Empty, HandleAsync).RequirePermission(Permissions.Structure.Manage);

        /// <summary>
        /// Creates a unit.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            SaveUnitCommand unit,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(unit), cancellationToken)).ToCreated(static id => $"{ApiEndpoints.Base}/organization-units/{id}");
    }
}
