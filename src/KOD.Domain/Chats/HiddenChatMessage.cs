namespace KOD.Domain.Chats;

/// <summary>
/// A message a member deleted only for themselves: the others still see it, the member gets an empty placeholder that
/// keeps the numbering of the conversation without gaps.
/// </summary>
public sealed class HiddenChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HiddenChatMessage"/> class.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="userId">The member who hid it.</param>
    /// <param name="moment">The current moment.</param>
    public HiddenChatMessage(Guid chatId, Guid messageId, Guid userId, DateTimeOffset moment)
    {
        ChatId = chatId;
        MessageId = messageId;
        UserId = userId;
        HiddenAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HiddenChatMessage"/> class for EF Core materialization.
    /// </summary>
    private HiddenChatMessage()
    {
    }

    /// <summary>
    /// Gets the chat.
    /// </summary>
    public Guid ChatId { get; private set; }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Gets the member who hid the message.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the moment of hiding.
    /// </summary>
    public DateTimeOffset HiddenAt { get; private set; }
}
