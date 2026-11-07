namespace ODC.Application.Features.Notifications;

/// <summary>
/// Delivers notifications to the open pages of their recipients at once (the web layer pushes them over SignalR). A
/// method never waits for the pages and never fails the change that caused it.
/// </summary>
public interface INotificationEvents
{
    /// <summary>
    /// A new notification (or one more event of a collected one).
    /// </summary>
    /// <param name="userId">The recipient.</param>
    /// <param name="notification">The notification.</param>
    /// <param name="unreadCount">The number of unread notifications of the recipient now.</param>
    void Received(Guid userId, NotificationDto notification, int unreadCount);

    /// <summary>
    /// Notifications were read or deleted (the other pages of the user update their badge).
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="unreadCount">The number of unread notifications of the user now.</param>
    void Changed(Guid userId, int unreadCount);
}
