using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Documents;
using KOD.Domain.News;
using KOD.Domain.Organization;
using KOD.Domain.Sessions;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Abstractions;

/// <summary>
/// The unit of work over the application's persistent state.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Gets the user accounts.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets the positions held by users.
    /// </summary>
    DbSet<StaffPosition> StaffPositions { get; }

    /// <summary>
    /// Gets the individual permission adjustments.
    /// </summary>
    DbSet<UserPermissionOverride> UserPermissionOverrides { get; }

    /// <summary>
    /// Gets the roles.
    /// </summary>
    DbSet<Role> Roles { get; }

    /// <summary>
    /// Gets the organization units.
    /// </summary>
    DbSet<OrganizationUnit> OrganizationUnits { get; }

    /// <summary>
    /// Gets the documents.
    /// </summary>
    DbSet<Document> Documents { get; }

    /// <summary>
    /// Gets the signing route steps.
    /// </summary>
    DbSet<DocumentSigner> DocumentSigners { get; }

    /// <summary>
    /// Gets the document signatures.
    /// </summary>
    DbSet<DocumentSignature> DocumentSignatures { get; }

    /// <summary>
    /// Gets the news articles.
    /// </summary>
    DbSet<NewsArticle> NewsArticles { get; }

    /// <summary>
    /// Gets the audit trail.
    /// </summary>
    DbSet<AuditEntry> AuditEntries { get; }

    /// <summary>
    /// Gets the server-side authentication sessions.
    /// </summary>
    DbSet<UserSession> UserSessions { get; }

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction using the configured execution strategy.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
