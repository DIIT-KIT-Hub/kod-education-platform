using System.ComponentModel.DataAnnotations;
using KOD.Domain.Chats;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/messages/{messageId}</c>.
/// </summary>
/// <param name="Body">The HTML of the editor.</param>
public sealed record ChatMessageRequest([Required][MaxLength(ChatMessage.BodyMaxLength)] string Body);
