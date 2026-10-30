using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Access;

/// <summary>
/// Default <see cref="IAccessScope"/>; caches the scopes for the lifetime of the scope (request or circuit).
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
internal sealed class AccessScope(IApplicationDbContext db, ICurrentUser currentUser) : IAccessScope
{
    /// <summary>
    /// The cached administration scopes by permission.
    /// </summary>
    private readonly Dictionary<string, AdministrationScope> _administrationScopes = new(StringComparer.Ordinal);

    /// <summary>
    /// The cached unit scope.
    /// </summary>
    private IReadOnlySet<Guid>? _unitScope;

    /// <summary>
    /// The cached organization scope.
    /// </summary>
    private IReadOnlySet<Guid>? _organizationScope;

    /// <summary>
    /// The cached positions of the current user.
    /// </summary>
    private List<PositionGrant>? _positions;

    /// <summary>
    /// The cached unit hierarchy.
    /// </summary>
    private UnitHierarchy? _hierarchy;

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetUnitScopeAsync(CancellationToken cancellationToken)
    {
        if (_unitScope is not null)
        {
            return _unitScope;
        }

        var positions = await GetPositionsAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return _unitScope = new HashSet<Guid>();
        }

        var hierarchy = await GetHierarchyAsync(cancellationToken);
        return _unitScope = hierarchy.WithDescendants(positions.Select(static p => p.UnitId));
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetOrganizationScopeAsync(CancellationToken cancellationToken)
    {
        if (_organizationScope is not null)
        {
            return _organizationScope;
        }

        var positions = await GetPositionsAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return _organizationScope = new HashSet<Guid>();
        }

        var hierarchy = await GetHierarchyAsync(cancellationToken);
        return _organizationScope = hierarchy.OrganizationsOf(positions.Select(static p => p.UnitId));
    }

    /// <inheritdoc />
    public async Task<AdministrationScope> GetAdministrationScopeAsync(string permission, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(permission);
        if (!currentUser.Has(permission))
        {
            return AdministrationScope.None;
        }

        if (currentUser.Has(Permissions.Administration.AllOrganizations))
        {
            return AdministrationScope.Cabinet;
        }

        if (_administrationScopes.TryGetValue(permission, out var cached))
        {
            return cached;
        }

        // The permission applies where a position grants it; an individual grant is not tied to a position and applies to all of them.
        var positions = await GetPositionsAsync(cancellationToken);
        var units = positions
            .Where(p => p.Grants(permission))
            .Select(static p => p.UnitId)
            .ToList();

        var scope = AdministrationScope.None;
        if (units.Count > 0)
        {
            var hierarchy = await GetHierarchyAsync(cancellationToken);
            scope = AdministrationScope.Of(hierarchy.OrganizationsOf(units));
        }

        _administrationScopes[permission] = scope;
        return scope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>?> GetDocumentUnitScopeAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Has(Permissions.Documents.ViewAll))
        {
            return null;
        }

        if (currentUser.Has(Permissions.Documents.ViewOrganization))
        {
            return await GetOrganizationScopeAsync(cancellationToken);
        }

        return currentUser.Has(Permissions.Documents.ViewUnit)
            ? await GetUnitScopeAsync(cancellationToken)
            : new HashSet<Guid>();
    }

    /// <inheritdoc />
    public async Task<IQueryable<Document>> VisibleDocumentsAsync(IQueryable<Document> query, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return query.Where(static _ => false);
        }

        var scope = await GetDocumentUnitScopeAsync(cancellationToken);
        if (scope is null)
        {
            return query;
        }

        if (scope.Count > 0)
        {
            var units = scope.ToList();
            return query.Where(d =>
                d.AuthorId == userId
                || d.Signers.Any(s => s.UserId == userId)
                || units.Contains(d.OrganizationUnitId));
        }

        return query.Where(d => d.AuthorId == userId || d.Signers.Any(s => s.UserId == userId));
    }

    /// <summary>
    /// Loads the positions of the current user with the permissions they grant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The positions.</returns>
    private async Task<List<PositionGrant>> GetPositionsAsync(CancellationToken cancellationToken)
    {
        if (_positions is not null)
        {
            return _positions;
        }

        if (currentUser.Id is not { } userId)
        {
            return _positions = [];
        }

        var positions = await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(static p => new
            {
                p.OrganizationUnitId,
                p.Role!.Code,
                Permissions = p.Role.Permissions.Where(static rp => rp.ClaimType == PermissionClaimTypes.Permission).Select(static rp => rp.ClaimValue!).ToList(),
            })
            .ToListAsync(cancellationToken);

        var granted = await db.UserPermissionOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.ClaimType == PermissionClaimTypes.Permission)
            .Select(static o => o.ClaimValue!)
            .ToListAsync(cancellationToken);

        return _positions =
        [
            .. positions.Select(p => new PositionGrant(
                p.OrganizationUnitId,
                p.Code == SystemRoles.Administrator,
                p.Permissions.Concat(granted).ToHashSet(StringComparer.Ordinal))),
        ];
    }

    /// <summary>
    /// Loads the unit hierarchy once per scope.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hierarchy.</returns>
    private async Task<UnitHierarchy> GetHierarchyAsync(CancellationToken cancellationToken)
        => _hierarchy ??= await UnitHierarchy.LoadAsync(db, cancellationToken);

    /// <summary>
    /// A position of the current user and the permissions it grants (its role's permissions and the individual grants).
    /// </summary>
    /// <param name="UnitId">The unit of the position.</param>
    /// <param name="IsAdministrator">Whether the role is the system administrator role, which grants every permission.</param>
    /// <param name="Permissions">The permissions granted by the role and individually.</param>
    private sealed record PositionGrant(Guid UnitId, bool IsAdministrator, IReadOnlySet<string> Permissions)
    {
        /// <summary>
        /// Determines whether the position grants a permission.
        /// </summary>
        /// <param name="permission">The permission code.</param>
        /// <returns><see langword="true"/> when granted.</returns>
        public bool Grants(string permission) => IsAdministrator || Permissions.Contains(permission);
    }
}
