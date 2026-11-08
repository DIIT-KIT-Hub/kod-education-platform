using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// The current user's notifications as the slices of this feature change them: finds the user's own notification and
/// saves changes, telling the user's other pages the new unread count.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="events">Delivers the unread count to the open pages.</param>
internal sealed class NotificationInbox(ApplicationDbContext db, ICurrentUser currentUser, INotificationEvents events)
{
    /// <summary>
    /// The error of a notification the current user does not have.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Сповіщення не знайдено."));

    /// <summary>
    /// Loads a notification of the current user for a change.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked notification, or <see langword="null"/>.</returns>
    public async Task<Notification?> FindOwnAsync(Guid id, CancellationToken cancellationToken)
        => currentUser.Id is { } me ? await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == me, cancellationToken) : null;

    /// <summary>
    /// Counts the unread notifications of the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number.</returns>
    public async Task<int> CountUnreadAsync(CancellationToken cancellationToken)
        => currentUser.Id is { } me ? await db.Notifications.CountAsync(n => n.UserId == me && n.ReadAt == null, cancellationToken) : 0;

    /// <summary>
    /// Saves the changes and tells the pages of the user the new unread count.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        if (currentUser.Id is { } me)
        {
            events.Changed(me, await CountUnreadAsync(cancellationToken));
        }
    }
}
