using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Chats.Domain;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/messages/{messageId}</c>.
/// </summary>
/// <param name="Body">The HTML of the editor.</param>
public sealed record ChatMessageRequest([Required][MaxLength(ChatMessage.BodyMaxLength)] string Body);
