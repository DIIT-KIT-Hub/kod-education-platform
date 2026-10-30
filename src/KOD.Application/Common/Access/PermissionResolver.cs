using KOD.Application.Abstractions;
using KOD.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Access;

/// <summary>
/// Default <see cref="IPermissionResolver"/> backed by the database.
/// </summary>
/// <param name="db">The database context.</param>
internal sealed class PermissionResolver(IApplicationDbContext db) : IPermissionResolver
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.Role!.Code,
                Permissions = p.Role.Permissions.Where(static rp => rp.ClaimType == PermissionClaimTypes.Permission).Select(static rp => rp.ClaimValue!),
            })
            .ToListAsync(cancellationToken);

        if (roles.Exists(static r => r.Code == SystemRoles.Administrator))
        {
            return Permissions.Codes;
        }

        var overrides = await db.UserPermissionOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(static o => new { o.ClaimType, o.ClaimValue })
            .ToListAsync(cancellationToken);

        var effective = roles.SelectMany(static r => r.Permissions).ToHashSet(StringComparer.Ordinal);
        effective.UnionWith(overrides.Where(static o => o.ClaimType == PermissionClaimTypes.Permission).Select(static o => o.ClaimValue!));
        effective.ExceptWith(overrides.Where(static o => o.ClaimType == PermissionClaimTypes.DeniedPermission).Select(static o => o.ClaimValue!));
        effective.IntersectWith(Permissions.Codes);

        return effective;
    }
}
