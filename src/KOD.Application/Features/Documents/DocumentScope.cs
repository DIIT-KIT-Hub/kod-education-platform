namespace KOD.Application.Features.Documents;

/// <summary>
/// Which documents a list shows.
/// </summary>
public enum DocumentScope
{
    /// <summary>Every document visible to the user.</summary>
    Accessible = 0,

    /// <summary>Documents waiting for the user's signature right now.</summary>
    AwaitingMe = 1,

    /// <summary>Documents authored by the user.</summary>
    Authored = 2,

    /// <summary>Documents where the user is on the signing route.</summary>
    Involved = 3,
}
