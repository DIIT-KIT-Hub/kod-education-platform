using ODC.Web.Shared.Domain.Audit;

namespace ODC.Web.Infrastructure.Auditing;

/// <summary>
/// The audit entries of the current request waiting for the next save of either database (scoped).
/// </summary>
internal sealed class PendingAudit
{
    /// <summary>
    /// The waiting entries.
    /// </summary>
    private readonly List<AuditEntry> _entries = [];

    /// <summary>
    /// Adds an entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    public void Add(AuditEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Takes every waiting entry.
    /// </summary>
    /// <returns>The entries.</returns>
    public List<AuditEntry> TakeAll()
    {
        List<AuditEntry> taken = [.. _entries];
        _entries.Clear();
        return taken;
    }
}
