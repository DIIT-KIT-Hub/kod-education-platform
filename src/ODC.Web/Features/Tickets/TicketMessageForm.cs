using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Tickets.Domain;

namespace ODC.Web.Features.Tickets;

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
}
