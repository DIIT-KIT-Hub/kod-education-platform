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
    /// The cached unit scope.
    /// </summary>
    private IReadOnlySet<Guid>? _unitScope;

    /// <summary>
    /// The cached organization scope.
    /// </summary>
    private IReadOnlySet<Guid>? _organizationScope;

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetUnitScopeAsync(CancellationToken cancellationToken)
    {
        if (_unitScope is not null)
        {
            return _unitScope;
        }

        var positions = await GetPositionUnitsAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return _unitScope = new HashSet<Guid>();
        }

        var links = await LoadLinksAsync(cancellationToken);
        return _unitScope = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), positions);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetOrganizationScopeAsync(CancellationToken cancellationToken)
    {
        if (_organizationScope is not null)
        {
            return _organizationScope;
        }

        var positions = await GetPositionUnitsAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return _organizationScope = new HashSet<Guid>();
        }

        var links = await LoadLinksAsync(cancellationToken);
        var parents = links.ToDictionary(static l => l.Id, static l => l.ParentId);
        var organizations = links.Where(static l => l.IsOrganization).Select(static l => l.Id).ToHashSet();
        var roots = positions.Where(parents.ContainsKey).Select(id => OrganizationTree.OrganizationOf(parents, organizations, id));
        return _organizationScope = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), roots);
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
    /// Loads the units of the current user's positions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unit identifiers.</returns>
    private async Task<List<Guid>> GetPositionUnitsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return [];
        }

        return await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(static p => p.OrganizationUnitId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the hierarchy links of every unit.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The links.</returns>
    private Task<List<UnitLink>> LoadLinksAsync(CancellationToken cancellationToken)
        => db.OrganizationUnits
            .AsNoTracking()
            .Select(static u => new UnitLink(u.Id, u.ParentId, u.Type!.IsOrganization))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// A unit with its parent.
    /// </summary>
    /// <param name="Id">The unit identifier.</param>
    /// <param name="ParentId">The parent identifier.</param>
    /// <param name="IsOrganization">Whether the unit is an organization.</param>
    private sealed record UnitLink(Guid Id, Guid? ParentId, bool IsOrganization);
}
