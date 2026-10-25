using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Access;

/// <summary>
/// Default <see cref="IAccessScope"/>; caches the unit scope for the lifetime of the scope (request or circuit).
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
internal sealed class AccessScope(IApplicationDbContext db, ICurrentUser currentUser) : IAccessScope
{
    /// <summary>
    /// The cached unit scope.
    /// </summary>
    private IReadOnlySet<Guid>? _unitScope;

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetUnitScopeAsync(CancellationToken cancellationToken)
    {
        if (_unitScope is not null)
        {
            return _unitScope;
        }

        if (currentUser.Id is not { } userId)
        {
            return _unitScope = new HashSet<Guid>();
        }

        var roots = await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(static p => p.OrganizationUnitId)
            .ToListAsync(cancellationToken);

        var links = await db.OrganizationUnits
            .AsNoTracking()
            .Select(static u => new { u.Id, u.ParentId })
            .ToListAsync(cancellationToken);

        return _unitScope = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), roots);
    }

    /// <inheritdoc />
    public async Task<IQueryable<Document>> VisibleDocumentsAsync(IQueryable<Document> query, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return query.Where(static _ => false);
        }

        if (currentUser.Has(Permissions.Documents.ViewAll))
        {
            return query;
        }

        if (currentUser.Has(Permissions.Documents.ViewUnit))
        {
            var units = (await GetUnitScopeAsync(cancellationToken)).ToList();
            return query.Where(d =>
                d.AuthorId == userId
                || d.Signers.Any(s => s.UserId == userId)
                || units.Contains(d.OrganizationUnitId));
        }

        return query.Where(d => d.AuthorId == userId || d.Signers.Any(s => s.UserId == userId));
    }
}
