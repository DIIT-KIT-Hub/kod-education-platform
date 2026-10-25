namespace KOD.Application.Abstractions;

/// <summary>
/// Writes entries to the audit trail.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Adds an audit entry on behalf of the current user. The entry is saved with the next <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="action">The action code.</param>
    /// <param name="entityType">The affected entity type.</param>
    /// <param name="entityId">The affected entity identifier.</param>
    /// <param name="details">The human-readable description.</param>
    void Add(string action, string? entityType = null, string? entityId = null, string? details = null);

    /// <summary>
    /// Writes an audit entry for an explicitly specified user and saves it immediately (used by authentication flows).
    /// </summary>
    /// <param name="action">The action code.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="userName">The user display name.</param>
    /// <param name="details">The human-readable description.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the entry is saved.</returns>
    Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken);
}
