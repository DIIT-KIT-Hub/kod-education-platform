using Microsoft.AspNetCore.Http.HttpResults;
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

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Adds a unit type to the catalog shared by every organization.
/// </summary>
public static class CreateUnitType
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Type">The new type.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Command(SaveUnitTypeCommand Type) : ICommand<Result<Guid>>;

    /// <summary>
    /// Validates and saves the type.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(MasterDbContext db, ICurrentUser currentUser, IAuditLogger audit) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var data = request.Type;
            if (UnitTypeCatalog.CheckEditor(currentUser) is { } denied)
            {
                return denied;
            }

            if (await UnitTypeCatalog.ValidateAsync(db, null, data, cancellationToken) is { } invalid)
            {
                return invalid;
            }

            return await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var type = new UnitType(data.Code, data.Name, data.IsOrganization);
                type.Update(data.Code, data.Name, data.Description, data.Icon, data.IsOrganization, data.SortOrder);
                db.UnitTypes.Add(type);
                audit.Add(AuditActions.UnitTypeChanged, nameof(UnitType), type.Id.ToString(), $"Створено тип {type.Code} «{type.Name}»");
                await db.SaveChangesAsync(cancellationToken);
                return type.Id;
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/unit-types</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnitTypes().MapPost(string.Empty, HandleAsync);

        /// <summary>
        /// Creates a unit type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            SaveUnitTypeCommand type,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(type), cancellationToken)).ToCreated(static id => $"{ApiEndpoints.Base}/unit-types/{id}");
    }
}
