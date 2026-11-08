using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets.Domain;

/// <summary>
/// Somebody changed the state of a request on purpose (automatic changes of an answer are part of the answer).
/// </summary>
/// <param name="TicketId">The request.</param>
/// <param name="ActorId">Who changed it.</param>
/// <param name="Status">The new state.</param>
public sealed record TicketStatusChanged(Guid TicketId, Guid ActorId, TicketStatus Status) : IDomainEvent;
