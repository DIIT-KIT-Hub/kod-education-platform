using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Lists the active staff of a unit: leaders first, then by name.
/// </summary>
public static class ListUnitStaff
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="UnitId">The unit.</param>
    /// <param name="Paging">The search and paging.</param>
    [RequirePermission(Permissions.Structure.View)]
    public sealed record Query(Guid UnitId, PageRequest Paging) : IQuery<Result<PagedResult<UnitStaffItem>>>;

    /// <summary>
    /// Reads the page.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Query, Result<PagedResult<UnitStaffItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<UnitStaffItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var positions = db.StaffPositions.AsNoTracking().Where(p => p.OrganizationUnitId == request.UnitId && p.User!.IsActive);
            if (request.Paging.Search is { } search)
            {
                var term = search;
                positions = positions.Where(p => p.User!.LastName.Contains(term) || p.User.FirstName.Contains(term));
            }

            return await positions
                .OrderByDescending(static p => p.Role!.IsLeadership)
                .ThenBy(static p => p.User!.LastName)
                .ThenBy(static p => p.User!.FirstName)
                .Select(static p => new UnitStaffItem(
                    p.UserId,
                    p.User!.LastName + " " + p.User.FirstName + (p.User.MiddleName != null ? " " + p.User.MiddleName : string.Empty),
                    p.Title ?? StoredText.Display(p.Role!.Name),
                    p.User.Email!,
                    p.User.PhoneNumber,
                    p.IsPrimary,
                    p.Role!.IsLeadership,
                    p.User.PhotoId))
                .ToPagedResultAsync(request.Paging, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/organization-units/{id}/staff</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapUnits().MapGet("{id:guid}/staff", HandleAsync);

        /// <summary>
        /// Lists the staff of a unit.
        /// </summary>
        /// <param name="id">The unit.</param>
        /// <param name="paging">The search and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<UnitStaffItem>>, ProblemHttpResult>> HandleAsync(
            Guid id,
            [AsParameters] PageRequest paging,
            IRequestHandler<Query, Result<PagedResult<UnitStaffItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id, paging), cancellationToken)).ToOk();
    }
}
