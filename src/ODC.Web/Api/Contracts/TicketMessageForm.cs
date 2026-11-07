using System.ComponentModel.DataAnnotations;
using ODC.Domain.Tickets;

namespace ODC.Web.Api.Contracts;

/// <summary>
/// Multipart form of <c>POST /api/v1/tickets/{id}/messages</c>.
/// </summary>
public sealed class TicketMessageForm
{
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    [MaxLength(TicketMessage.BodyMaxLength)]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the attached files.
    /// </summary>
    [MaxLength(TicketMessage.MaxAttachments)]
    public List<IFormFile> Files { get; set; } = [];
}
