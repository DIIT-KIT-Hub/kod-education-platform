namespace ODC.Domain.Tickets;

/// <summary>
/// The kind of an entry in the history of a request.
/// </summary>
public enum TicketMessageKind
{
    /// <summary>Text written by the author or a handler, with attachments (the first one describes the request).</summary>
    Comment = 0,

    /// <summary>The state changed (<see cref="TicketMessage.Status"/>).</summary>
    StatusChanged = 1,

    /// <summary>The request was assigned to a handler (the name is the text) or released (empty text).</summary>
    Assigned = 2,
}
