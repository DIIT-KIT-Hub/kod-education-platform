using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Chats.Domain;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Multipart form of <c>POST /api/v1/chats/{id}/messages</c>.
/// </summary>
public sealed class SendChatMessageForm
{
    /// <summary>
    /// Gets or sets the identifier the browser gave the message (a resend with it lands only once).
    /// </summary>
    [Required]
    public Guid ClientId { get; set; }

    /// <summary>
    /// Gets or sets the HTML of the editor.
    /// </summary>
    [MaxLength(ChatMessage.BodyMaxLength)]
    public string? Body { get; set; }
}
