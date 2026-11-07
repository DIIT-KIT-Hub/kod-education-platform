using ODC.Domain.Users;

namespace ODC.Domain.Chats;

/// <summary>
/// A person in a chat and how far they have read it.
/// </summary>
public sealed class ChatMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMember"/> class.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The person.</param>
    /// <param name="role">The role.</param>
    /// <param name="joinedAt">The moment of joining.</param>
    /// <param name="lastReadSequence">The number of the last message the person has read.</param>
    internal ChatMember(Guid chatId, Guid userId, ChatMemberRole role, DateTimeOffset joinedAt, long lastReadSequence = 0)
    {
        ChatId = chatId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
        LastReadSequence = lastReadSequence;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMember"/> class for EF Core materialization.
    /// </summary>
    private ChatMember()
    {
    }

    /// <summary>
    /// Gets the chat.
    /// </summary>
    public Guid ChatId { get; private set; }

    /// <summary>
    /// Gets the person.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the person.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the role.
    /// </summary>
    public ChatMemberRole Role { get; private set; }

    /// <summary>
    /// Gets the moment of joining.
    /// </summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>
    /// Gets the number of the last message the person has read.
    /// </summary>
    public long LastReadSequence { get; private set; }

    /// <summary>
    /// Gets the number of the last message the person cleared from their history (0 when nothing was cleared): older
    /// messages are no longer shown to them, and a one-to-one chat leaves their list until a newer message arrives.
    /// </summary>
    public long ClearedSequence { get; private set; }

    /// <summary>
    /// Clears the history for this person up to the newest message, which also counts as read.
    /// </summary>
    /// <param name="lastSequence">The number of the newest message of the chat.</param>
    public void ClearHistory(long lastSequence)
    {
        ClearedSequence = Math.Max(ClearedSequence, lastSequence);
        MarkRead(lastSequence);
    }

    /// <summary>
    /// Moves the read position forward (never back: another tab may have read further).
    /// </summary>
    /// <param name="sequence">The number of the last read message.</param>
    /// <returns><see langword="true"/> when the position moved.</returns>
    public bool MarkRead(long sequence)
    {
        if (sequence <= LastReadSequence)
        {
            return false;
        }

        LastReadSequence = sequence;
        return true;
    }

    /// <summary>
    /// Makes the member the owner of the chat.
    /// </summary>
    internal void MakeOwner() => Role = ChatMemberRole.Owner;
}
