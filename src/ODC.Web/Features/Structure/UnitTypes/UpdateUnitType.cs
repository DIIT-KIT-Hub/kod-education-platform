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
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Replaces a unit type of the catalog.
/// </summary>
public static class UpdateUnitType
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The type.</param>
    /// <param name="Type">The new data.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Command(Guid Id, SaveUnitTypeCommand Type) : ICommand<Result>;

    /// <summary>
    /// Validates and saves the type.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="across">Reads every organization (the units of the type).</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(MasterDbContext db, IAcrossOrganizations across, ICurrentUser currentUser, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var data = request.Type;
            if (UnitTypeCatalog.CheckEditor(currentUser) is { } denied)
            {
                return denied;
            }

            var type = await db.UnitTypes.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (type is null)
            {
                return UnitTypeCatalog.NotFound();
            }

            if (await UnitTypeCatalog.ValidateAsync(db, request.Id, data, cancellationToken) is { } invalid)
            {
                return invalid;
            }

            if (type.IsOrganization && !data.IsOrganization
                && (await across.EveryAsync(organization => organization.OrganizationUnits.AnyAsync(u => u.TypeId == request.Id && u.ParentId == null, cancellationToken), cancellationToken)).Exists(static p => p.Value))
            {
                return Error.Conflict(Texts.T("Цей тип мають організації верхнього рівня, тому він має лишатися організацією. Спершу змініть їхній тип або вкладіть їх в іншу організацію."));
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                type.Update(data.Code, data.Name, data.Description, data.Icon, data.IsOrganization, data.SortOrder);
                audit.Add(AuditActions.UnitTypeChanged, nameof(UnitType), type.Id.ToString(), $"Змінено тип {type.Code} «{type.Name}»");
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/unit-types/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnitTypes().MapPut("{id:guid}", HandleAsync);

        /// <summary>
        /// Replaces a unit type.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            SaveUnitTypeCommand type,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, type), cancellationToken)).ToNoContent();
    }
}
