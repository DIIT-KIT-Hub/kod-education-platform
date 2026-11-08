using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Shared.Access;

/// <summary>
/// Finds the people who hold a permission, the way <see cref="PermissionResolver"/> computes it for one user: role
/// permissions of any position and individual grants, minus individual denials; a system administrator holds every one.
/// </summary>
public static class PermissionHolders
{
    /// <param name="db">The database context.</param>
    extension(ApplicationDbContext db)
    {
        /// <summary>
        /// Selects the active accounts that hold a permission (translatable to SQL).
        /// </summary>
        /// <param name="permission">The permission code.</param>
        /// <returns>The query.</returns>
        public IQueryable<User> UsersWith(string permission) => db.Users.Where(u => u.IsActive
            && (u.Positions.Any(static p => p.Role!.Code == SystemRoles.Administrator)
                || (!db.UserPermissionOverrides.Any(o => o.UserId == u.Id && o.ClaimType == PermissionClaimTypes.DeniedPermission && o.ClaimValue == permission)
                    && (u.Positions.Any(p => p.Role!.Permissions.Any(rp => rp.ClaimType == PermissionClaimTypes.Permission && rp.ClaimValue == permission))
                        || db.UserPermissionOverrides.Any(o => o.UserId == u.Id && o.ClaimType == PermissionClaimTypes.Permission && o.ClaimValue == permission)))));
    }
}
