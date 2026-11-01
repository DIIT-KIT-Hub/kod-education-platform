using System.ComponentModel.DataAnnotations;
using KOD.Domain.Tickets;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/tickets/{id}/status</c>.
/// </summary>
/// <param name="Status">The new state.</param>
public sealed record TicketStatusRequest([Required] TicketStatus Status);
