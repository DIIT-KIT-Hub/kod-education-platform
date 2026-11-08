using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Admin.Roles;
using ODC.Web.Features.Structure.Units;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Lists the accounts with a position in the organizations the current user administers, with the lookups of the grid
/// filters and the position editor (units, roles).
/// </summary>
public static class ListUsers
{
    /// <summary>
    /// The filters and paging.
    /// </summary>
    [RequirePermission(Permissions.Users.View)]
    public sealed record Query : UserQuery, IQuery<Result<PagedResult<UserListItem>>>;

    /// <summary>
    /// Filters, sorts and pages the accounts; the shown position is the first one inside the scope.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, IAccessScope accessScope, UserDirectory directory, TimeProvider timeProvider)
        : IRequestHandler<Query, Result<PagedResult<UserListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<UserListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.View, cancellationToken);
            var all = scope.IsCabinetWide;
            var units = UserDirectory.ScopeUnits(scope);
            var now = timeProvider.GetUtcNow();
            var users = await directory.FilterAsync(UserDirectory.InScope(db.Users.AsNoTracking(), scope), request, cancellationToken);
            users = request.SortBy switch
            {
                "email" => users.OrderByDirection(static u => u.Email, request.IsDescending),
                "lastSignIn" => users.OrderByDirection(static u => u.LastSignInAt, request.IsDescending),
                _ => request.IsDescending
                    ? users.OrderByDescending(static u => u.LastName).ThenByDescending(static u => u.FirstName)
                    : users.OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName),
            };

            return await users
                .Select(u => new UserListItem(
                    u.Id,
                    u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                    u.Email!,
                    StoredText.PositionTitle(
                        u.Positions.Where(p => all || units.Contains(p.OrganizationUnitId)).OrderByDescending(static p => p.IsPrimary).ThenBy(static p => p.Id).Select(static p => p.Title).FirstOrDefault(),
                        u.Positions.Where(p => all || units.Contains(p.OrganizationUnitId)).OrderByDescending(static p => p.IsPrimary).ThenBy(static p => p.Id).Select(static p => p.Role!.Name).FirstOrDefault()),
                    u.Positions.Where(p => all || units.Contains(p.OrganizationUnitId)).OrderByDescending(static p => p.IsPrimary).ThenBy(static p => p.Id).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault(),
                    u.IsActive,
                    u.PasswordHash == null,
                    u.TwoFactorEnabled,
                    u.LockoutEnd != null && u.LockoutEnd > now,
                    u.LastSignInAt,
                    u.PhotoId))
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/users</c>, <c>GET /api/v1/users/units</c> and <c>GET /api/v1/users/roles</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var users = UserDirectory.MapUsers(api);
            users.MapGet(string.Empty, HandleAsync);
            users.MapGet("units", UnitsAsync);
            users.MapGet("roles", RolesAsync);
        }

        /// <summary>
        /// Lists user accounts.
        /// </summary>
        /// <param name="query">The filters and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<UserListItem>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, Result<PagedResult<UserListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();

        /// <summary>
        /// Lists the units of the user filters (organizations the user sees) or of the position editor (organizations the
        /// user manages), in tree order.
        /// </summary>
        /// <param name="units">Lists the units of a permission scope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="manage">Whether the units of the position editor are listed.</param>
        /// <returns>The units.</returns>
        private static async Task<Ok<IReadOnlyList<UnitOption>>> UnitsAsync(
            IRequestHandler<ListUnitOptions.Query, IReadOnlyList<UnitOption>> units,
            CancellationToken cancellationToken,
            bool manage = false)
            => TypedResults.Ok(await units.HandleAsync(new ListUnitOptions.Query(manage ? Permissions.Users.Manage : Permissions.Users.View), cancellationToken));

        /// <summary>
        /// Lists the roles for the filters and the position editor.
        /// </summary>
        /// <param name="roles">Lists the roles.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The roles.</returns>
        private static async Task<Ok<IReadOnlyList<RoleOption>>> RolesAsync(IRequestHandler<ListRoleOptions.Query, IReadOnlyList<RoleOption>> roles, CancellationToken cancellationToken)
            => TypedResults.Ok(await roles.HandleAsync(new ListRoleOptions.Query(), cancellationToken));
    }
}
