using ODC.Application.Common.Models;
using ODC.Domain.Chats;

namespace ODC.Application.Features.Chats;

/// <summary>
/// A member of a chat.
/// </summary>
/// <param name="User">The person.</param>
/// <param name="Role">The role.</param>
/// <param name="Position">The primary position with its unit.</param>
/// <param name="LastReadSequence">The number of the last message the person has read ("seen").</param>
/// <param name="IsActive">Whether the account is active (a blocked person still reads nothing).</param>
public sealed record ChatMemberDto(
    UserRef User,
    ChatMemberRole Role,
    string? Position,
    long LastReadSequence,
    bool IsActive);
