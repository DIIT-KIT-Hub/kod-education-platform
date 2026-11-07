using System.ComponentModel.DataAnnotations;
using ODC.Domain.Tickets;

namespace ODC.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/tickets/{id}/status</c>.
/// </summary>
/// <param name="Status">The new state.</param>
public sealed record TicketStatusRequest([Required] TicketStatus Status);
