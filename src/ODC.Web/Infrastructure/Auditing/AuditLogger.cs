using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;

namespace ODC.Web.Infrastructure.Auditing;

/// <summary>
/// Writes the audit trail. <see cref="Add"/> keeps the entry until the request saves its changes: the database that
/// saves next takes it (<see cref="PendingAuditInterceptor"/>), so an organization's action is audited in the
/// organization's database together with the change, and an account or catalog change in the master database.
/// <see cref="WriteAsync"/> (sign-in events) writes to the master database at once.
/// </summary>
/// <param name="master">The master database.</param>
/// <param name="pending">The entries waiting for the next save.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class AuditLogger(MasterDbContext master, PendingAudit pending, ICurrentUser currentUser, TimeProvider timeProvider) : IAuditLogger
{
    /// <inheritdoc />
    public void Add(string action, string? entityType = null, string? entityId = null, string? details = null)
        => pending.Add(new AuditEntry(
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
        master.AuditEntries.Add(new AuditEntry(timeProvider.GetUtcNow(), action, userId, userName, null, userId?.ToString(), details, currentUser.IpAddress));
        await master.SaveChangesAsync(cancellationToken);
    }
}
