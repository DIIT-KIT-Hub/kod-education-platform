using KOD.Application.Common.Results;

namespace KOD.Application.Features.Profile;

/// <summary>
/// Self-service use cases of the signed-in user.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The profile.</returns>
    Task<Result<ProfileDto>> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the contact phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdatePhoneAsync(string? phoneNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the password and clears the "must change password" flag.
    /// </summary>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a new set of recovery codes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The codes (shown once).</returns>
    Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists the current user's active sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sessions.</returns>
    Task<Result<IReadOnlyList<SessionDto>>> GetSessionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ends one of the current user's sessions.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Ends all sessions except the current one.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of ended sessions.</returns>
    Task<Result<int>> RevokeOtherSessionsAsync(CancellationToken cancellationToken);
}
