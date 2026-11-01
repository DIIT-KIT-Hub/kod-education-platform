using KOD.Application.Features.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace KOD.Web.Realtime;

/// <summary>
/// Pushes notifications and unread counts to every open page of their recipient (<see cref="LiveHub"/> users, identified
/// by the token subject).
/// </summary>
/// <param name="hub">The hub context.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class LiveNotificationEvents(IHubContext<LiveHub, ILiveClient> hub, ILogger<LiveNotificationEvents> logger) : INotificationEvents
{
    /// <inheritdoc />
    public void Received(Guid userId, NotificationDto notification, int unreadCount)
        => Send(userId, client => client.NotificationReceived(notification, unreadCount));

    /// <inheritdoc />
    public void Changed(Guid userId, int unreadCount)
        => Send(userId, client => client.NotificationsChanged(unreadCount));

    /// <summary>
    /// Logs a notification that could not be sent.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The recipient.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "A notification was not sent to the open pages of user {UserId}")]
    private static partial void LogSendFailed(ILogger logger, Guid userId, Exception exception);

    /// <summary>
    /// Sends a message to the pages of a user without waiting for them.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="send">Sends the message to the user's connections.</param>
    private void Send(Guid userId, Func<ILiveClient, Task> send)
    {
        // A slow or gone page must not delay the request that made the change.
        _ = SendAsync(userId, send);
    }

    /// <summary>
    /// Sends a message and logs a failure instead of throwing.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="send">Sends the message to the user's connections.</param>
    /// <returns>A task that completes when sent.</returns>
    private async Task SendAsync(Guid userId, Func<ILiveClient, Task> send)
    {
        try
        {
            await send(hub.Clients.User(userId.ToString()));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogSendFailed(logger, userId, exception);
        }
    }
}
