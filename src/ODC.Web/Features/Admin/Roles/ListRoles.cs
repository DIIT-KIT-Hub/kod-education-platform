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

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Lists the roles with the number of permissions and holders.
/// </summary>
public static class ListRoles
{
    /// <summary>
    /// The search, sorting and paging.
    /// </summary>
    [RequirePermission(Permissions.Roles.Manage)]
    public sealed record Query : PageRequest, IQuery<Result<PagedResult<RoleListItem>>>;

    /// <summary>
    /// Reads the page.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="organization">The organization's database (positions with the role).</param>
    internal sealed class Handler(MasterDbContext db, OrganizationDbContext organization) : IRequestHandler<Query, Result<PagedResult<RoleListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<RoleListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var roles = db.Roles.AsNoTracking();
            if (request.Search is { } search)
            {
                var term = search;
                roles = roles.Where(r => r.Name.Contains(term) || r.Code.Contains(term));
            }

            roles = request.SortBy switch
            {
                "code" => roles.OrderByDirection(static r => r.Code, request.IsDescending),
                "name" => roles.OrderByDirection(static r => r.Name, request.IsDescending),
                _ => roles.OrderByDescending(static r => r.IsSystem).ThenBy(static r => r.CreatedAt),
            };

            var page = await roles
                .Select(r => new RoleListItem(
                    r.Id,
                    r.Code,
                    StoredText.Display(r.Name),
                    StoredText.Display(r.Description),
                    r.IsSystem,
                    r.IsLeadership,
                    r.Permissions.Count,
                    0))
                .ToPagedResultAsync(request, cancellationToken);
            var ids = page.Items.Select(static r => r.Id).ToList();
            var holders = await organization.StaffPositions
                .Where(p => ids.Contains(p.RoleId))
                .GroupBy(static p => p.RoleId)
                .Select(static g => new { g.Key, Count = g.Select(static p => p.UserId).Distinct().Count() })
                .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);
            return page with { Items = [.. page.Items.Select(r => r with { UserCount = holders.GetValueOrDefault(r.Id) })] };
        }
    }

    /// <summary>
    /// <c>GET /api/v1/roles</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapRoles().MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists roles.
        /// </summary>
        /// <param name="query">The search, sorting and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<RoleListItem>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, Result<PagedResult<RoleListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }
}
