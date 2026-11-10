using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Audit;

namespace ODC.Web.Infrastructure.Auditing;

/// <summary>
/// Saves the waiting audit entries (<see cref="PendingAudit"/>) with the changes of whichever database saves next, in
/// the same transaction (scoped: one per request). What a platform administrator does in an organization they do not
/// work in (<see cref="OrganizationAccess.Platform"/>) is audited twice: in that organization, and after its save in the
/// platform's trail of the master database with the organization's code in front of the details, so the platform sees
/// every use of its powers even if the organization's trail is lost.
/// </summary>
/// <param name="pending">The waiting entries.</param>
/// <param name="organization">The organization of the request.</param>
/// <param name="scopes">Creates the scope of the master copy.</param>
internal sealed class PendingAuditInterceptor(PendingAudit pending, OrganizationContext organization, IServiceScopeFactory scopes) : SaveChangesInterceptor
{
    /// <summary>
    /// The entries saved in an organization that the platform's trail still has to get.
    /// </summary>
    private List<AuditEntry> _platform = [];

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

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await CopyToPlatformAsync(cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        CopyToPlatformAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _platform = [];
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _platform = [];
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>
    /// Adds the waiting entries to the context that is saving (and remembers the platform's copies).
    /// </summary>
    /// <param name="context">The context.</param>
    private void Attach(DbContext? context)
    {
        if (context?.Model.FindEntityType(typeof(AuditEntry)) is null)
        {
            return;
        }

        var entries = pending.TakeAll();
        context.Set<AuditEntry>().AddRange(entries);
        if (context is OrganizationDbContext && organization is { Access: OrganizationAccess.Platform, Current: { } here })
        {
            _platform.AddRange(entries.Select(e => new AuditEntry(
                e.OccurredAt,
                e.Action,
                e.UserId,
                e.UserName,
                e.EntityType,
                e.EntityId,
                string.Create(CultureInfo.InvariantCulture, $"[{here.Code}] {e.Details}"),
                e.IpAddress)));
        }
    }

    /// <summary>
    /// Writes the platform's copies after the organization saved its entries.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    private async Task CopyToPlatformAsync(CancellationToken cancellationToken)
    {
        if (_platform.Count == 0)
        {
            return;
        }

        var copies = _platform;
        _platform = [];
        await using var scope = scopes.CreateAsyncScope();
        var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        master.AuditEntries.AddRange(copies);
        await master.SaveChangesAsync(cancellationToken);
    }
}
