using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Gets a unit type for its editor (names as stored, not translated).
/// </summary>
public static class GetUnitType
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The type.</param>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Query(Guid Id) : IQuery<Result<UnitTypeItem>>;

    /// <summary>
    /// Reads the type.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="organization">The organization's database (units of the type).</param>
    internal sealed class Handler(MasterDbContext db, OrganizationDbContext organization) : IRequestHandler<Query, Result<UnitTypeItem>>
    {
        /// <inheritdoc />
        public async Task<Result<UnitTypeItem>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var type = (await UnitTypeCatalog.ItemsAsync(db, organization, request.Id, cancellationToken)).FirstOrDefault();
            return type is null ? UnitTypeCatalog.NotFound() : type;
        }
    }

    /// <summary>
    /// <c>GET /api/v1/unit-types/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnitTypes().MapGet("{id:guid}", HandleAsync);

        /// <summary>
        /// Gets a unit type.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The type.</returns>
        private static async Task<Results<Ok<UnitTypeItem>, ProblemHttpResult>> HandleAsync(
            Guid id,
            IRequestHandler<Query, Result<UnitTypeItem>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
