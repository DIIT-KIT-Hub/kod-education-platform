using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Tickets.Domain;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Multipart form of <c>POST /api/v1/tickets</c>.
/// </summary>
public sealed class SubmitTicketForm
{
    /// <summary>
    /// Gets or sets what the request is about.
    /// </summary>
    [Required]
    public TicketCategory Category { get; set; }

    /// <summary>
    /// Gets or sets how urgent it is.
    /// </summary>
    [Required]
    public TicketPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    [Required]
    [MaxLength(Ticket.SubjectMaxLength)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [Required]
    [MaxLength(TicketMessage.BodyMaxLength)]
    public string Description { get; set; } = string.Empty;
}
