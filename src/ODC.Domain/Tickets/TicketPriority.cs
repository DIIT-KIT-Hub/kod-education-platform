namespace ODC.Domain.Tickets;

/// <summary>
/// How urgent a request is for its author.
/// </summary>
public enum TicketPriority
{
    /// <summary>Can wait.</summary>
    Low = 0,

    /// <summary>The usual urgency.</summary>
    Normal = 1,

    /// <summary>Gets in the way of work.</summary>
    High = 2,

    /// <summary>Work has stopped.</summary>
    Critical = 3,
}
