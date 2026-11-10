using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Default <see cref="INotifier"/>.
/// </summary>
/// <param name="db">The database context (its earlier changes are already saved by the caller).</param>
/// <param name="events">Delivers notifications to the open pages.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
/// <param name="organization">The organization the notifications are saved in (named in what the pages receive).</param>
/// <param name="across">Reaches the other organizations of the recipients.</param>
internal sealed partial class Notifier(
    OrganizationDbContext db,
    INotificationEvents events,
    TimeProvider timeProvider,
    ILogger<Notifier> logger,
    OrganizationContext organization,
    IAcrossOrganizations across) : INotifier
{
    /// <inheritdoc />
    public async Task NotifyAsync(NotificationDraft draft, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(recipients);
        var wanted = recipients.Where(id => id != draft.ActorId).Distinct().ToList();
        if (wanted.Count == 0)
        {
            return;
        }

        try
        {
            var users = await db.Members.Where(u => wanted.Contains(u.Id) && u.IsActive).Select(static u => u.Id).ToListAsync(cancellationToken);
            if (draft.OnceSince is { } since)
            {
                var told = await db.Notifications
                    .Where(n => users.Contains(n.UserId) && n.EntityId == draft.EntityId && n.Kind == draft.Kind && n.CreatedAt >= since)
                    .Select(static n => n.UserId)
                    .ToListAsync(cancellationToken);
                users = [.. users.Except(told)];
            }

            if (users.Count == 0)
            {
                return;
            }

            var collected = draft.Collect
                ? await db.Notifications
                    .Where(n => users.Contains(n.UserId) && n.EntityId == draft.EntityId && n.Kind == draft.Kind && n.ReadAt == null)
                    .ToListAsync(cancellationToken)
                : [];
            var moment = timeProvider.GetUtcNow();
            var changed = new List<Guid>();
            foreach (var user in users)
            {
                if (collected.Find(n => n.UserId == user) is { } existing)
                {
                    existing.Repeat(draft.ActorId, draft.Subject, draft.Preview, moment);
                    changed.Add(existing.Id);
                    continue;
                }

                var notification = new Notification(user, draft.Kind, draft.ActorId, draft.EntityId, draft.Subject, draft.Preview, draft.Code, draft.Link, moment);
                db.Notifications.Add(notification);
                changed.Add(notification.Id);
            }

            await db.SaveChangesAsync(cancellationToken);
            await PublishAsync(changed, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            db.DiscardChanges();
            LogNotifyFailed(logger, draft.Kind, draft.EntityId, exception);
        }
    }

    /// <inheritdoc />
    public async Task NotifyWhereTheyWorkAsync(NotificationDraft draft, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        var here = organization.Current;
        var groups = new Dictionary<Guid, (Organization Organization, List<Guid> People)>();
        foreach (var recipient in recipients.Distinct())
        {
            if ((await across.HomeOfAsync(recipient, cancellationToken) ?? here) is not { } home)
            {
                continue;
            }

            if (!groups.TryGetValue(home.Id, out var group))
            {
                group = (home, []);
                groups[home.Id] = group;
            }

            group.People.Add(recipient);
        }

        foreach (var (home, people) in groups.Values)
        {
            if (home.Id == here?.Id)
            {
                await NotifyAsync(draft, people, cancellationToken);
            }
            else
            {
                await across.RunInAsync(home, services => services.GetRequiredService<INotifier>().NotifyAsync(draft, people, cancellationToken));
            }
        }
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(Guid userId, Guid entityId, CancellationToken cancellationToken)
    {
        try
        {
            var changed = false;
            var moment = timeProvider.GetUtcNow();
            await across.EachAsync(
                userId,
                async other =>
                {
                    var unread = await other.Notifications.Where(n => n.UserId == userId && n.EntityId == entityId && n.ReadAt == null).ToListAsync(cancellationToken);
                    if (unread.Count > 0)
                    {
                        unread.ForEach(n => n.MarkRead(moment));
                        await other.SaveChangesAsync(cancellationToken);
                        changed = true;
                    }
                },
                cancellationToken);
            if (changed)
            {
                var counts = await across.GatherAsync(userId, other => other.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken), cancellationToken);
                events.Changed(userId, counts.Sum(static c => c.Value));
            }
        }
        catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
        {
            // Read twice at once (two pages): the other request marked them.
            db.DiscardChanges();
            LogMarkReadFailed(logger, entityId, exception);
        }
    }

    /// <summary>
    /// Logs notifications that could not be saved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="kind">What they tell about.</param>
    /// <param name="entityId">What they concern.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Notifications {Kind} about {EntityId} were not saved")]
    private static partial void LogNotifyFailed(ILogger logger, NotificationKind kind, Guid entityId, Exception exception);

    /// <summary>
    /// Logs notifications that could not be marked read.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityId">What they concern.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Notifications about {EntityId} were not marked read")]
    private static partial void LogMarkReadFailed(ILogger logger, Guid entityId, Exception exception);

    /// <summary>
    /// Sends saved notifications to the pages of their recipients with their unread counts in this organization (the
    /// pages read the count of all organizations again).
    /// </summary>
    /// <param name="ids">The notifications.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when handed over.</returns>
    private async Task PublishAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        var owners = await db.Notifications
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(static n => new { n.Id, n.UserId })
            .ToDictionaryAsync(static n => n.Id, static n => n.UserId, cancellationToken);
        var users = owners.Values.Distinct().ToList();
        var unread = await db.Notifications
            .Where(n => users.Contains(n.UserId) && n.ReadAt == null)
            .GroupBy(static n => n.UserId)
            .Select(static g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);
        var here = organization.Require();
        var notifications = (await db.Notifications.AsNoTracking().Where(n => ids.Contains(n.Id)).Select(NotificationRows.Dto).ToListAsync(cancellationToken))
            .Select(n => NotificationRows.In(n, here));
        foreach (var notification in notifications)
        {
            var user = owners[notification.Id];
            events.Received(user, notification, unread.GetValueOrDefault(user));
        }
    }
}
