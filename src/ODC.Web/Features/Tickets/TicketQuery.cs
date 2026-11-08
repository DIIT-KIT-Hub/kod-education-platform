using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Search, filters, sorting (<c>number</c>, <c>created</c>, <c>priority</c>, <c>status</c>; by default the last activity,
/// newest first) and paging of a request list.
/// </summary>
public record TicketQuery : PageRequest
{
    /// <summary>
    /// Gets the state; <see langword="null"/> lists all of them.
    /// </summary>
    public TicketStatus? Status { get; init; }

    /// <summary>
    /// Gets the category; <see langword="null"/> lists all of them.
    /// </summary>
    public TicketCategory? Category { get; init; }

    /// <summary>
    /// Gets a value indicating whether only the open requests (not resolved and not closed) are listed.
    /// </summary>
    public bool? OpenOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether only the requests assigned to the current user are listed (handlers).
    /// </summary>
    public bool? AssignedToMe { get; init; }
}
