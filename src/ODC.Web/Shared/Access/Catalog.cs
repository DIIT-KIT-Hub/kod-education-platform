using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Shared.Access;

/// <summary>
/// The catalogs every organization shares, read from the master database once per request: the roles with their
/// permissions and the unit types. The organizations' data refers to them by identifier (a position's role, a unit's
/// type), so code on the organization's side looks them up here instead of joining across databases.
/// </summary>
/// <param name="master">The master database.</param>
internal sealed class Catalog(MasterDbContext master)
{
    /// <summary>
    /// The roles, once loaded.
    /// </summary>
    private IReadOnlyDictionary<Guid, CatalogRole>? _roles;

    /// <summary>
    /// The unit types, once loaded.
    /// </summary>
    private IReadOnlyDictionary<Guid, CatalogUnitType>? _unitTypes;

    /// <summary>
    /// Gets the roles by identifier.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles.</returns>
    public async Task<IReadOnlyDictionary<Guid, CatalogRole>> RolesAsync(CancellationToken cancellationToken)
    {
        if (_roles is not null)
        {
            return _roles;
        }

        var roles = await master.Roles
            .AsNoTracking()
            .Select(static r => new
            {
                r.Id,
                r.Code,
                Name = r.Name ?? r.Code,
                r.IsSystem,
                r.IsLeadership,
                Permissions = r.Permissions.Where(static p => p.ClaimType == PermissionClaimTypes.Permission).Select(static p => p.ClaimValue!).ToList(),
            })
            .ToListAsync(cancellationToken);
        return _roles = roles.ToDictionary(
            static r => r.Id,
            static r => new CatalogRole(r.Id, r.Code, r.Name, r.IsSystem, r.IsLeadership, r.Permissions.ToHashSet(StringComparer.Ordinal)));
    }

    /// <summary>
    /// Gets the unit types by identifier.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unit types.</returns>
    public async Task<IReadOnlyDictionary<Guid, CatalogUnitType>> UnitTypesAsync(CancellationToken cancellationToken)
        => _unitTypes ??= await master.UnitTypes
            .AsNoTracking()
            .Select(static t => new CatalogUnitType(t.Id, t.Code, t.Name, t.Icon, t.IsOrganization, t.SortOrder))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

    /// <summary>
    /// Gets the identifiers of the unit types whose units are organizations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifiers.</returns>
    public async Task<List<Guid>> OrganizationTypeIdsAsync(CancellationToken cancellationToken)
        => [.. (await UnitTypesAsync(cancellationToken)).Values.Where(static t => t.IsOrganization).Select(static t => t.Id)];

    /// <summary>
    /// Gets the identifiers of the roles that grant a permission (the administrator's role grants every permission).
    /// </summary>
    /// <param name="permission">The permission code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifiers.</returns>
    public async Task<List<Guid>> RoleIdsGrantingAsync(string permission, CancellationToken cancellationToken)
        => [.. (await RolesAsync(cancellationToken)).Values.Where(r => r.Grants(permission)).Select(static r => r.Id)];
}
