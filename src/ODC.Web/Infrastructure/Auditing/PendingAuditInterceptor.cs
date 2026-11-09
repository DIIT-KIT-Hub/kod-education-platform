using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ODC.Web.Shared.Domain.Audit;

namespace ODC.Web.Infrastructure.Auditing;

/// <summary>
/// Saves the waiting audit entries (<see cref="PendingAudit"/>) with the changes of whichever database saves next, in
/// the same transaction (scoped: one per request).
/// </summary>
/// <param name="pending">The waiting entries.</param>
internal sealed class PendingAuditInterceptor(PendingAudit pending) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Attach(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Attach(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Adds the waiting entries to the context that is saving.
    /// </summary>
    /// <param name="context">The context.</param>
    private void Attach(DbContext? context)
    {
        if (context?.Model.FindEntityType(typeof(AuditEntry)) is null)
        {
            return;
        }

        context.Set<AuditEntry>().AddRange(pending.TakeAll());
    }
}
