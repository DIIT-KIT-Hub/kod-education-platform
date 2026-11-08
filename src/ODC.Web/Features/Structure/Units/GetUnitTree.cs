using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// The organization tree: organizations and their units.
/// </summary>
public static class GetUnitTree
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="IncludeInactive">Whether inactive units are included.</param>
    [RequirePermission(Permissions.Structure.View)]
    public sealed record Query(bool IncludeInactive) : IQuery<Result<IReadOnlyList<UnitTreeNode>>>;

    /// <summary>
    /// Builds the tree.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Query, Result<IReadOnlyList<UnitTreeNode>>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<UnitTreeNode>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var units = await UnitTree.LoadAsync(db, cancellationToken);
            return Result<IReadOnlyList<UnitTreeNode>>.Success(UnitTree.Build(units, null, request.IncludeInactive));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Gets the organization tree.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="includeInactive">Whether inactive units are included.</param>
        /// <returns>The top-level organizations with their subtrees.</returns>
        private static async Task<Results<Ok<IReadOnlyList<UnitTreeNode>>, ProblemHttpResult>> HandleAsync(
            IRequestHandler<Query, Result<IReadOnlyList<UnitTreeNode>>> handler,
            CancellationToken cancellationToken,
            bool includeInactive = false)
            => (await handler.HandleAsync(new Query(includeInactive), cancellationToken)).ToOk();
    }
}
