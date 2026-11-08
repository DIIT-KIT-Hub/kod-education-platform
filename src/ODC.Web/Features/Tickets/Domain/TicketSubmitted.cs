using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets.Domain;

/// <summary>
/// A new request was submitted.
/// </summary>
/// <param name="TicketId">The request.</param>
/// <param name="AuthorId">The author.</param>
/// <param name="Description">The description (its first message).</param>
public sealed record TicketSubmitted(Guid TicketId, Guid AuthorId, string Description) : IDomainEvent;
