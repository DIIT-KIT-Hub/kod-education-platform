using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Lists the unit type catalog (organization, department, division, sector, branch and custom kinds) with its usage.
/// </summary>
public static class ListUnitTypes
{
    /// <summary>
    /// The request.
    /// </summary>
    [RequirePermission(Permissions.Structure.Manage)]
    public sealed record Query : IQuery<Result<IReadOnlyList<UnitTypeItem>>>;

    /// <summary>
    /// Reads the types; seeded names are shown in the user's language.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Query, Result<IReadOnlyList<UnitTypeItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<UnitTypeItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var types = await UnitTypeCatalog.Items(db).ToListAsync(cancellationToken);
            return Result<IReadOnlyList<UnitTypeItem>>.Success([.. types.Select(static t => t with { Name = StoredText.Display(t.Name), Description = StoredText.Display(t.Description) })]);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/unit-types</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnitTypes().MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists the unit types with their usage.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The types in display order.</returns>
        private static async Task<Results<Ok<IReadOnlyList<UnitTypeItem>>, ProblemHttpResult>> HandleAsync(
            IRequestHandler<Query, Result<IReadOnlyList<UnitTypeItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(), cancellationToken)).ToOk();
    }
}
