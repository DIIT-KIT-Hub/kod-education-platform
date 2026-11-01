namespace KOD.Domain.Tickets;

/// <summary>
/// The state of a request.
/// </summary>
public enum TicketStatus
{
    /// <summary>Submitted, nobody has handled it yet.</summary>
    New = 0,

    /// <summary>Somebody works on it.</summary>
    InProgress = 1,

    /// <summary>The handler waits for an answer of the author.</summary>
    WaitingForAuthor = 2,

    /// <summary>Done; an answer of the author opens it again.</summary>
    Resolved = 3,

    /// <summary>Closed for good (withdrawn, declined or a duplicate): nobody can answer it.</summary>
    Closed = 4,
}
