using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Gets an account with its positions, permissions and what the current user may change in it.
/// </summary>
public static class GetUser
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    [RequirePermission(Permissions.Users.View)]
    public sealed record Query(Guid Id) : IQuery<Result<UserDetails>>;

    /// <summary>
    /// Reads the account when it has a position in the scope.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="permissions">Resolves effective permissions.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        IPermissionResolver permissions,
        IAccessScope accessScope,
        TimeProvider timeProvider) : IRequestHandler<Query, Result<UserDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<UserDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            var user = await db.Users
                .AsNoTracking()
                .Include(static u => u.Positions).ThenInclude(static p => p.OrganizationUnit)
                .Include(static u => u.Positions).ThenInclude(static p => p.Role)
                .Include(static u => u.PermissionOverrides)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.View, cancellationToken);
            if (user is null || !scope.Overlaps(user.Positions.Select(static p => p.OrganizationUnitId)))
            {
                return UserDirectory.NotFound();
            }

            var now = timeProvider.GetUtcNow();
            var effective = await permissions.GetEffectivePermissionsAsync(id, cancellationToken);
            var hierarchy = await UnitHierarchy.LoadAsync(db, cancellationToken);
            var manageScope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
            var securityScope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Security, cancellationToken);
            var units = user.Positions.Select(static p => p.OrganizationUnitId).ToList();
            var withinHierarchy = id == currentUser.Id || effective.All(currentUser.Has);

            return new UserDetails(
                user.Id,
                user.Email!,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.FullName,
                user.PhoneNumber,
                user.IsActive,
                user.TwoFactorEnabled,
                user.AwaitsActivation,
                user.LockoutEnd > now,
                user.CreatedAt,
                user.LastSignInAt,
                user.Positions
                    .OrderByDescending(static p => p.IsPrimary)
                    .Select(p =>
                    {
                        var organization = hierarchy.OrganizationOf(p.OrganizationUnitId);
                        return new UserPositionDto(
                            p.OrganizationUnitId,
                            p.OrganizationUnit!.Name,
                            new UnitRef(organization, hierarchy.Names.GetValueOrDefault(organization, string.Empty)),
                            p.RoleId,
                            StoredText.Display(p.Role!.Name),
                            p.Title,
                            p.IsPrimary,
                            manageScope.Contains(p.OrganizationUnitId));
                    })
                    .ToList(),
                [.. UserDirectory.Overrides(user, PermissionClaimTypes.Permission)],
                [.. UserDirectory.Overrides(user, PermissionClaimTypes.DeniedPermission)],
                [.. effective],
                CanEditAccount: withinHierarchy && manageScope.Covers(units),
                CanEditPositions: withinHierarchy && manageScope.Overlaps(units),
                CanManageSecurity: withinHierarchy && securityScope.Covers(units),
                PhotoId: user.PhotoId);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/users/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => UserDirectory.MapUsers(api).MapGet("{id:guid}", HandleAsync);

        /// <summary>
        /// Gets a user account.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The account.</returns>
        private static async Task<Results<Ok<UserDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<UserDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
