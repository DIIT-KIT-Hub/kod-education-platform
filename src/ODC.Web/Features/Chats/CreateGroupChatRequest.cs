using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Chats.Domain;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>POST /api/v1/chats</c>: a group chat.
/// </summary>
/// <param name="Title">The title, or empty to show the members' names.</param>
/// <param name="MemberIds">The other members.</param>
public sealed record CreateGroupChatRequest(
    [MaxLength(Chat.TitleMaxLength)] string? Title,
    [Required][MaxLength(Chat.MaxMembers)] IReadOnlyList<Guid> MemberIds);
