using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Infrastructure.Persistence;

namespace KOD.Infrastructure.Auditing;

/// <summary>
/// Writes audit entries through the scoped database context.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class AuditLogger(ApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IAuditLogger
{
    /// <inheritdoc />
    public void Add(string action, string? entityType = null, string? entityId = null, string? details = null)
        => db.AuditEntries.Add(new AuditEntry(
            timeProvider.GetUtcNow(),
            action,
            currentUser.Id,
            currentUser.DisplayName,
            entityType,
            entityId,
            details,
            currentUser.IpAddress));

    /// <inheritdoc />
    public async Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken)
    {
        db.AuditEntries.Add(new AuditEntry(timeProvider.GetUtcNow(), action, userId, userName, null, userId?.ToString(), details, currentUser.IpAddress));
        await db.SaveChangesAsync(cancellationToken);
    }
}
