using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets.Domain;

/// <summary>
/// Somebody assigned a request to a handler on purpose (taking a request by answering it is part of the answer).
/// </summary>
/// <param name="TicketId">The request.</param>
/// <param name="ActorId">Who assigned it.</param>
/// <param name="AssigneeId">The handler.</param>
public sealed record TicketAssigned(Guid TicketId, Guid ActorId, Guid AssigneeId) : IDomainEvent;
