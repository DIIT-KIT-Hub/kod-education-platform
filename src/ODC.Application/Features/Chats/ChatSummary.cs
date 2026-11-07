using ODC.Application.Common.Models;
using ODC.Domain.Chats;

namespace ODC.Application.Features.Chats;

/// <summary>
/// A chat in the list of the current user's chats.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Title">The title of a group chat (<see langword="null"/>: the client shows the members' names).</param>
/// <param name="Members">The other members (up to four, for the avatars and the name of an untitled chat).</param>
/// <param name="MemberCount">The number of members, the current user included.</param>
/// <param name="LastMessage">The newest message.</param>
/// <param name="LastActivityAt">The moment of the newest message, or of the creation.</param>
/// <param name="LastSequence">The number of the newest message.</param>
/// <param name="LastReadSequence">The number of the last message the current user has read.</param>
/// <param name="UnreadCount">The number of messages of others the current user has not read.</param>
public sealed record ChatSummary(
    Guid Id,
    ChatKind Kind,
    string? Title,
    IReadOnlyList<UserRef> Members,
    int MemberCount,
    ChatPreview? LastMessage,
    DateTimeOffset LastActivityAt,
    long LastSequence,
    long LastReadSequence,
    int UnreadCount);
