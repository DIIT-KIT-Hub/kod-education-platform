using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Tickets.Domain;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Body of <c>PUT /api/v1/tickets/{id}/status</c>.
/// </summary>
/// <param name="Status">The new state.</param>
public sealed record TicketStatusRequest([Required] TicketStatus Status);
