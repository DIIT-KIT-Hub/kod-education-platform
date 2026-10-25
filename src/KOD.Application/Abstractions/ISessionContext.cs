namespace KOD.Application.Abstractions;

/// <summary>
/// Gives access to the identifier of the current server-side session.
/// </summary>
public interface ISessionContext
{
    /// <summary>
    /// Gets the identifier of the session of the current request, if any.
    /// </summary>
    Guid? CurrentSessionId { get; }
}
