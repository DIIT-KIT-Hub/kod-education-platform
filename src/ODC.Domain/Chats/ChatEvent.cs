namespace ODC.Domain.Chats;

/// <summary>
/// What a system message of a chat tells (the client shows it in the reader's language).
/// </summary>
public enum ChatEvent
{
    /// <summary>The group chat was created.</summary>
    Created,

    /// <summary>The group chat got a new title (<see cref="ChatMessage.Text"/> holds it).</summary>
    Renamed,

    /// <summary>People were added (<see cref="ChatMessage.Text"/> holds their names).</summary>
    MembersAdded,

    /// <summary>A person was removed (<see cref="ChatMessage.Text"/> holds the name).</summary>
    MemberRemoved,

    /// <summary>The author of the message left the chat.</summary>
    MemberLeft,
}
