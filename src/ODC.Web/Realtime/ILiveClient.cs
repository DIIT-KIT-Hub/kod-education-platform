using ODC.Web.Features.Chats;
using ODC.Web.Features.Notifications;
using ODC.Web.Shared.Models;

namespace ODC.Web.Realtime;

/// <summary>
/// The messages <see cref="LiveHub"/> sends to the pages. News signals only tell that something changed (the page loads
/// it through the REST API); chat messages carry the message itself, so it appears at once — it goes only to the
/// members the server read from the database.
/// </summary>
public interface ILiveClient
{
    /// <summary>
    /// The reactions or comments of an article changed.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task NewsDiscussionChanged(Guid articleId);

    /// <summary>
    /// A new message in a chat of the user.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatMessageAdded(ChatMessageDto message);

    /// <summary>
    /// A message was edited, deleted or got a reaction.
    /// </summary>
    /// <param name="message">The message as it is now.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatMessageChanged(ChatMessageDto message);

    /// <summary>
    /// The title or the members of a chat changed, or a new chat appeared.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatChanged(Guid chatId);

    /// <summary>
    /// The user is no longer a member of a chat.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatRemoved(Guid chatId);

    /// <summary>
    /// The user cleared the history of a chat for themselves (on another page).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatHistoryCleared(Guid chatId);

    /// <summary>
    /// A member read a chat up to a message.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The member.</param>
    /// <param name="sequence">The number of the last read message.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatRead(Guid chatId, Guid userId, long sequence);

    /// <summary>
    /// A member is typing in a chat (repeated every few seconds while they type).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="user">The member.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task ChatTyping(Guid chatId, UserRef user);

    /// <summary>
    /// A new notification of the user (or one more message of a chat in its notification).
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="unreadCount">The number of unread notifications now.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task NotificationReceived(NotificationDto notification, int unreadCount);

    /// <summary>
    /// Notifications of the user were read or deleted on another page.
    /// </summary>
    /// <param name="unreadCount">The number of unread notifications now.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task NotificationsChanged(int unreadCount);
}
