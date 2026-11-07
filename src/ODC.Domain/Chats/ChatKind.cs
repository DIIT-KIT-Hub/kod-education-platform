namespace ODC.Domain.Chats;

/// <summary>
/// The kind of a chat.
/// </summary>
public enum ChatKind
{
    /// <summary>A one-to-one chat of two people (at most one per pair).</summary>
    Direct,

    /// <summary>A group chat with a title and any number of members.</summary>
    Group,
}
