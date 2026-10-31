using KOD.Domain.Chats;

namespace KOD.Application.Features.Chats;

/// <summary>
/// A chat with its members.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Title">The title of a group chat.</param>
/// <param name="Members">The members, the owner first, then by name.</param>
/// <param name="LastSequence">The number of the newest message.</param>
/// <param name="MyRole">The role of the current user.</param>
public sealed record ChatDetails(
    Guid Id,
    ChatKind Kind,
    string? Title,
    IReadOnlyList<ChatMemberDto> Members,
    long LastSequence,
    ChatMemberRole MyRole);
