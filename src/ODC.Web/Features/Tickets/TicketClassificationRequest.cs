using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Tickets.Domain;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Body of <c>PUT /api/v1/tickets/{id}/classification</c>.
/// </summary>
/// <param name="Category">What the request is about.</param>
/// <param name="Priority">How urgent it is.</param>
public sealed record TicketClassificationRequest([Required] TicketCategory Category, [Required] TicketPriority Priority);
