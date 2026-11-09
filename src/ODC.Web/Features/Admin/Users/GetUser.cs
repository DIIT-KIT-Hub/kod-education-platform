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
    /// <param name="db">The master database (the account).</param>
    /// <param name="organization">The organization's database (individual permissions, units).</param>
    /// <param name="catalog">The shared catalogs (which unit types are organizations).</param>
    /// <param name="directory">The positions of people.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="permissions">Resolves effective permissions.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        MasterDbContext db,
        OrganizationDbContext organization,
        Catalog catalog,
        PositionDirectory directory,
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
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            var positions = await directory.AllAsync(id, cancellationToken);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.View, cancellationToken);
            if (user is null || !scope.Overlaps(positions.Select(static p => p.UnitId)))
            {
                return UserDirectory.NotFound();
            }

            var now = timeProvider.GetUtcNow();
            var effective = await permissions.GetEffectivePermissionsAsync(id, cancellationToken);
            var hierarchy = await UnitHierarchy.LoadAsync(organization, catalog, cancellationToken);
            var member = await organization.Members.AsNoTracking().Include(static m => m.PermissionOverrides).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            var manageScope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
            var securityScope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Security, cancellationToken);
            var units = positions.Select(static p => p.UnitId).ToList();
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
                positions
                    .Select(p =>
                    {
                        var owner = hierarchy.OrganizationOf(p.UnitId);
                        return new UserPositionDto(
                            p.UnitId,
                            p.UnitName,
                            new UnitRef(owner, hierarchy.Names.GetValueOrDefault(owner, string.Empty)),
                            p.RoleId,
                            StoredText.Display(p.RoleName) ?? p.RoleName,
                            p.Title,
                            p.IsPrimary,
                            manageScope.Contains(p.UnitId));
                    })
                    .ToList(),
                member is null ? [] : [.. UserDirectory.Overrides(member, PermissionClaimTypes.Permission)],
                member is null ? [] : [.. UserDirectory.Overrides(member, PermissionClaimTypes.DeniedPermission)],
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
