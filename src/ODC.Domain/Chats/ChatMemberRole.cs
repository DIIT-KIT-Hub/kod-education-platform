namespace ODC.Domain.Chats;

/// <summary>
/// The role of a member in a group chat.
/// </summary>
public enum ChatMemberRole
{
    /// <summary>Writes, renames the chat and adds people.</summary>
    Member,

    /// <summary>Also removes people (the creator; passes to the longest member when the owner leaves).</summary>
    Owner,
}
