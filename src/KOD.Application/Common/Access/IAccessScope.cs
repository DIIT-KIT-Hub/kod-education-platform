using KOD.Domain.Documents;

namespace KOD.Application.Common.Access;

/// <summary>
/// Data-level access rules of the current user.
/// </summary>
public interface IAccessScope
{
    /// <summary>
    /// Returns the units of the current user's positions together with all their descendants.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unit identifiers.</returns>
    Task<IReadOnlySet<Guid>> GetUnitScopeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Restricts a document query to the documents visible to the current user.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The restricted query.</returns>
    Task<IQueryable<Document>> VisibleDocumentsAsync(IQueryable<Document> query, CancellationToken cancellationToken);
}
