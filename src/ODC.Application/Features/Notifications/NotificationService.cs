using Microsoft.EntityFrameworkCore;
using ODC.Application.Abstractions;
using ODC.Application.Common.Paging;
using ODC.Application.Common.Results;
using ODC.Domain.Localization;
using ODC.Domain.Notifications;

namespace ODC.Application.Features.Notifications;

/// <summary>
/// Default <see cref="INotificationService"/>. Every change tells the other pages of the user the new unread count.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="events">Delivers the unread count to the open pages.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class NotificationService(IApplicationDbContext db, ICurrentUser currentUser, INotificationEvents events, TimeProvider timeProvider) : INotificationService
{
    /// <inheritdoc />
    public async Task<PagedResult<NotificationDto>> ListAsync(NotificationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (currentUser.Id is not { } me)
        {
            return PagedResult<NotificationDto>.Empty(query);
        }

        var notifications = db.Notifications.AsNoTracking().Where(n => n.UserId == me);
        if (query.UnreadOnly)
        {
            notifications = notifications.Where(static n => n.ReadAt == null);
        }

        return await notifications
            .OrderByDescending(static n => n.CreatedAt)
            .ThenByDescending(static n => n.Id)
            .Select(NotificationRows.Dto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountUnreadAsync(CancellationToken cancellationToken)
        => currentUser.Id is { } me ? await db.Notifications.CountAsync(n => n.UserId == me && n.ReadAt == null, cancellationToken) : 0;

    /// <inheritdoc />
    public async Task<Result> MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await OwnAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        if (notification.MarkRead(timeProvider.GetUtcNow()))
        {
            await SaveAsync(cancellationToken);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task MarkAllReadAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return;
        }

        var unread = await db.Notifications.Where(n => n.UserId == me && n.ReadAt == null).ToListAsync(cancellationToken);
        if (unread.Count > 0)
        {
            var moment = timeProvider.GetUtcNow();
            unread.ForEach(n => n.MarkRead(moment));
            await SaveAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await OwnAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        db.Notifications.Remove(notification);
        await SaveAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task DeleteReadAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return;
        }

        var read = await db.Notifications.Where(n => n.UserId == me && n.ReadAt != null).ToListAsync(cancellationToken);
        if (read.Count > 0)
        {
            db.Notifications.RemoveRange(read);
            await SaveAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The error of a notification the current user does not have.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error NotFound() => Error.NotFound(Texts.T("Сповіщення не знайдено."));

    /// <summary>
    /// Loads a notification of the current user for a change.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked notification, or <see langword="null"/>.</returns>
    private async Task<Notification?> OwnAsync(Guid id, CancellationToken cancellationToken)
        => currentUser.Id is { } me ? await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == me, cancellationToken) : null;

    /// <summary>
    /// Saves the changes and tells the pages of the user the new unread count.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        if (currentUser.Id is { } me)
        {
            events.Changed(me, await db.Notifications.CountAsync(n => n.UserId == me && n.ReadAt == null, cancellationToken));
        }
    }
}
