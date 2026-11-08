using System.ComponentModel.DataAnnotations;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>POST /api/v1/chats/{id}/forward</c>.
/// </summary>
/// <param name="SourceChatId">The chat of the messages.</param>
/// <param name="MessageIds">The messages to forward.</param>
public sealed record ForwardChatMessagesRequest(Guid SourceChatId, [Required][MinLength(1)][MaxLength(100)] IReadOnlyList<Guid> MessageIds);
