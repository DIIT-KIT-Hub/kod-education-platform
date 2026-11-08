using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Chats.Domain;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>POST /api/v1/chats/{id}/members</c>.
/// </summary>
/// <param name="UserIds">The people to add.</param>
public sealed record ChatMembersRequest([Required][MinLength(1)][MaxLength(Chat.MaxMembers)] IReadOnlyList<Guid> UserIds);
