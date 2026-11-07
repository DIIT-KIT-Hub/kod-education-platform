using System.ComponentModel.DataAnnotations;
using ODC.Domain.Chats;

namespace ODC.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/messages/{messageId}</c>.
/// </summary>
/// <param name="Body">The HTML of the editor.</param>
public sealed record ChatMessageRequest([Required][MaxLength(ChatMessage.BodyMaxLength)] string Body);
