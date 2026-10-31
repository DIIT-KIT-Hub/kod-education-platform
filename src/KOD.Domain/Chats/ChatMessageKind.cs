namespace KOD.Domain.Chats;

/// <summary>
/// The kind of a chat message.
/// </summary>
public enum ChatMessageKind
{
    /// <summary>A message written by a member (text and attachments).</summary>
    Text,

    /// <summary>A note of the chat itself (created, renamed, people added or removed); see <see cref="ChatEvent"/>.</summary>
    System,
}
