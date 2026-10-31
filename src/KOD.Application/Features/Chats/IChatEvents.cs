namespace KOD.Application.Features.Chats;

/// <summary>
/// Delivers chat changes to the open pages of the members at once. The recipients always come from the database
/// (the members at the moment of the change), so a removed member stops receiving messages immediately. The web layer
/// pushes them over SignalR (with several servers, a SignalR backplane carries them to all of them). A method never
/// waits for the pages and never fails the change that caused it.
/// </summary>
public interface IChatEvents
{
    /// <summary>
    /// A new message was sent.
    /// </summary>
    /// <param name="recipients">The members.</param>
    /// <param name="message">The message.</param>
    void MessageAdded(IReadOnlyCollection<Guid> recipients, ChatMessageDto message);

    /// <summary>
    /// A message was edited, deleted or got a reaction.
    /// </summary>
    /// <param name="recipients">The members.</param>
    /// <param name="message">The message as it is now.</param>
    void MessageChanged(IReadOnlyCollection<Guid> recipients, ChatMessageDto message);

    /// <summary>
    /// The title or the members of a chat changed, or a chat appeared for the recipients.
    /// </summary>
    /// <param name="recipients">The members.</param>
    /// <param name="chatId">The chat.</param>
    void ChatChanged(IReadOnlyCollection<Guid> recipients, Guid chatId);

    /// <summary>
    /// The recipients are no longer members of a chat (they left or were removed).
    /// </summary>
    /// <param name="recipients">The former members.</param>
    /// <param name="chatId">The chat.</param>
    void ChatRemoved(IReadOnlyCollection<Guid> recipients, Guid chatId);

    /// <summary>
    /// A member read a chat up to a message.
    /// </summary>
    /// <param name="recipients">The members.</param>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The member who read.</param>
    /// <param name="sequence">The number of the last read message.</param>
    void Read(IReadOnlyCollection<Guid> recipients, Guid chatId, Guid userId, long sequence);
}
