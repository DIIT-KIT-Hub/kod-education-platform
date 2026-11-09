using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Shared.Access;

/// <summary>
/// Default <see cref="IPermissionResolver"/>: the roles of the person's positions in the organization's database, looked
/// up in the master database's catalog, plus the individual adjustments.
/// </summary>
/// <param name="db">The organization's database.</param>
/// <param name="catalog">The shared catalogs.</param>
internal sealed class PermissionResolver(OrganizationDbContext db, Catalog catalog) : IPermissionResolver
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(static p => p.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var catalogRoles = await catalog.RolesAsync(cancellationToken);
        var roles = roleIds.Select(id => catalogRoles.GetValueOrDefault(id)).OfType<CatalogRole>().ToList();
        if (roles.Exists(static r => r.IsAdministrator))
        {
            return Permissions.Codes;
        }

        var overrides = await db.UserPermissionOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(static o => new { o.ClaimType, o.ClaimValue })
            .ToListAsync(cancellationToken);

        var effective = roles.SelectMany(static r => r.Permissions).ToHashSet(StringComparer.Ordinal);
        effective.UnionWith(overrides.Where(static o => o.ClaimType == PermissionClaimTypes.Permission).Select(static o => o.ClaimValue));
        effective.ExceptWith(overrides.Where(static o => o.ClaimType == PermissionClaimTypes.DeniedPermission).Select(static o => o.ClaimValue));
        effective.IntersectWith(Permissions.Codes);
        return effective;
    }
}
