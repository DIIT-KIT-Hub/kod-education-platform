using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Shared.Access;

/// <summary>
/// Finds the people who hold a permission (e.g. the handlers of requests): a position whose role grants it (the
/// administrator's role grants every permission) or an individual grant, and no individual denial.
/// </summary>
internal static class PermissionHolders
{
    /// <param name="db">The organization's database.</param>
    extension(OrganizationDbContext db)
    {
        /// <summary>
        /// The active members who hold a permission.
        /// </summary>
        /// <param name="catalog">The shared catalogs (the roles).</param>
        /// <param name="permission">The permission code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query of the members.</returns>
        public async Task<IQueryable<Member>> MembersWithAsync(Catalog catalog, string permission, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            var roles = await catalog.RolesAsync(cancellationToken);
            var administrators = roles.Values.Where(static r => r.IsAdministrator).Select(static r => r.Id).ToList();
            var granting = roles.Values.Where(r => r.Permissions.Contains(permission)).Select(static r => r.Id).ToList();
            return db.Members.Where(m => m.IsActive
                && (m.Positions.Any(p => administrators.Contains(p.RoleId))
                    || (!m.PermissionOverrides.Any(o => o.ClaimType == PermissionClaimTypes.DeniedPermission && o.ClaimValue == permission)
                        && (m.Positions.Any(p => granting.Contains(p.RoleId))
                            || m.PermissionOverrides.Any(o => o.ClaimType == PermissionClaimTypes.Permission && o.ClaimValue == permission)))));
        }

        /// <summary>
        /// The identifiers of the active members who hold a permission.
        /// </summary>
        /// <param name="catalog">The shared catalogs (the roles).</param>
        /// <param name="permission">The permission code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identifiers.</returns>
        public async Task<List<Guid>> MemberIdsWithAsync(Catalog catalog, string permission, CancellationToken cancellationToken)
            => await (await db.MembersWithAsync(catalog, permission, cancellationToken)).Select(static m => m.Id).ToListAsync(cancellationToken);
    }
}
