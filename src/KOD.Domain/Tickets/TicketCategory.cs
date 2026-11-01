namespace KOD.Domain.Tickets;

/// <summary>
/// What a request is about.
/// </summary>
public enum TicketCategory
{
    /// <summary>Something does not work as it should.</summary>
    Bug = 0,

    /// <summary>An idea how to make the cabinet better.</summary>
    Suggestion = 1,

    /// <summary>A question about how to do something.</summary>
    Question = 2,

    /// <summary>Access rights, a role or an account.</summary>
    Access = 3,

    /// <summary>Anything else.</summary>
    Other = 4,
}
