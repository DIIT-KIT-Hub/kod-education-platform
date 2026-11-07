using Microsoft.AspNetCore.SignalR;
using ODC.Application.Features.Chats;

namespace ODC.Web.Realtime;

/// <summary>
/// Pushes chat changes to every open page of the members (<see cref="LiveHub"/> users, identified by the token subject).
/// </summary>
/// <param name="hub">The hub context.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class LiveChatEvents(IHubContext<LiveHub, ILiveClient> hub, ILogger<LiveChatEvents> logger) : IChatEvents
{
    /// <inheritdoc />
    public void MessageAdded(IReadOnlyCollection<Guid> recipients, ChatMessageDto message)
        => Send(recipients, message.ChatId, client => client.ChatMessageAdded(message));

    /// <inheritdoc />
    public void MessageChanged(IReadOnlyCollection<Guid> recipients, ChatMessageDto message)
        => Send(recipients, message.ChatId, client => client.ChatMessageChanged(message));

    /// <inheritdoc />
    public void ChatChanged(IReadOnlyCollection<Guid> recipients, Guid chatId)
        => Send(recipients, chatId, client => client.ChatChanged(chatId));

    /// <inheritdoc />
    public void ChatRemoved(IReadOnlyCollection<Guid> recipients, Guid chatId)
        => Send(recipients, chatId, client => client.ChatRemoved(chatId));

    /// <inheritdoc />
    public void HistoryCleared(IReadOnlyCollection<Guid> recipients, Guid chatId)
        => Send(recipients, chatId, client => client.ChatHistoryCleared(chatId));

    /// <inheritdoc />
    public void Read(IReadOnlyCollection<Guid> recipients, Guid chatId, Guid userId, long sequence)
        => Send(recipients, chatId, client => client.ChatRead(chatId, userId, sequence));

    /// <summary>
    /// Logs a change that could not be sent.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="chatId">The chat.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "A change of chat {ChatId} was not sent to the open pages")]
    private static partial void LogSendFailed(ILogger logger, Guid chatId, Exception exception);

    /// <summary>
    /// Sends a message to the pages of the recipients without waiting for them.
    /// </summary>
    /// <param name="recipients">The users.</param>
    /// <param name="chatId">The chat (for the log).</param>
    /// <param name="send">Sends the message to the recipients' connections.</param>
    private void Send(IReadOnlyCollection<Guid> recipients, Guid chatId, Func<ILiveClient, Task> send)
    {
        if (recipients.Count > 0)
        {
            // A slow or gone page must not delay the request that made the change.
            _ = SendAsync(recipients, chatId, send);
        }
    }

    /// <summary>
    /// Sends a message and logs a failure instead of throwing.
    /// </summary>
    /// <param name="recipients">The users.</param>
    /// <param name="chatId">The chat (for the log).</param>
    /// <param name="send">Sends the message to the recipients' connections.</param>
    /// <returns>A task that completes when sent.</returns>
    private async Task SendAsync(IReadOnlyCollection<Guid> recipients, Guid chatId, Func<ILiveClient, Task> send)
    {
        try
        {
            await send(hub.Clients.Users([.. recipients.Select(static id => id.ToString())]));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogSendFailed(logger, chatId, exception);
        }
    }
}
