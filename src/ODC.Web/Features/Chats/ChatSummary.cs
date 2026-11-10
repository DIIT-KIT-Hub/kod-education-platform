using ODC.Web.Features.Chats.Domain;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Chats;

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
/// <param name="OrganizationId">The organization the chat lives in (the list gathers the chats of all the user's
/// organizations).</param>
/// <param name="OrganizationCode">The code of that organization (the chat opens at <c>/o/{code}/chats/{id}</c>).</param>
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
    int UnreadCount,
    Guid OrganizationId,
    string OrganizationCode);
