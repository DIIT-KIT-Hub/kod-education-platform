using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets.Domain;

/// <summary>
/// Somebody wrote in a request (the author or a handler).
/// </summary>
/// <param name="TicketId">The request.</param>
/// <param name="AuthorId">The writer.</param>
/// <param name="Preview">The text, or <see langword="null"/> for a message with files only.</param>
public sealed record TicketCommented(Guid TicketId, Guid AuthorId, string? Preview) : IDomainEvent;
