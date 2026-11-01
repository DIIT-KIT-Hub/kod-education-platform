using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Notifications;

/// <summary>
/// The notifications of the current user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Lists the notifications, the newest first.
    /// </summary>
    /// <param name="query">The paging and filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    Task<PagedResult<NotificationDto>> ListAsync(NotificationQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the unread notifications (the badge).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number.</returns>
    Task<int> CountUnreadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks a notification read.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> MarkReadAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Marks every notification read.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task MarkAllReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a notification.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the read notifications.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task DeleteReadAsync(CancellationToken cancellationToken);
}
