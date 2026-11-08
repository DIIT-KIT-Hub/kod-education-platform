namespace ODC.Web.Features.Tickets;

/// <summary>
/// Body of <c>PUT /api/v1/tickets/{id}/assignee</c>.
/// </summary>
/// <param name="AssigneeId">The handler, or <see langword="null"/> to release the request.</param>
public sealed record TicketAssigneeRequest(Guid? AssigneeId);
