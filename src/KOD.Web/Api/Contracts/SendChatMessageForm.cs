using System.ComponentModel.DataAnnotations;
using KOD.Domain.Chats;

namespace KOD.Web.Api.Contracts;

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

    /// <summary>
    /// Gets or sets the attached files and photos.
    /// </summary>
    [MaxLength(ChatMessage.MaxAttachments)]
    public List<IFormFile> Files { get; set; } = [];
}
