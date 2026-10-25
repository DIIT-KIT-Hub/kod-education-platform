namespace KOD.Infrastructure.Identity;

/// <summary>
/// Checks whether a server-side session is still alive. Interactive circuits use it to stop working as soon as their
/// session is revoked (sign-out, "end other sessions", administrator actions) or expires.
/// </summary>
public interface IUserSessionValidator
{
    /// <summary>
    /// Determines whether the session exists, belongs to the user and has not expired.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the session is alive.</returns>
    Task<bool> IsAliveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}
