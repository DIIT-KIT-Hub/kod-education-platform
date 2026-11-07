using ODC.Application.Common.Results;
using ODC.Domain.Users;

namespace ODC.Application.Abstractions;

/// <summary>
/// Credential operations backed by ASP.NET Core Identity.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Creates an account without a password: the owner activates it from the sign-in page.
    /// </summary>
    /// <param name="user">The new user.</param>
    /// <returns>The result.</returns>
    Task<Result> CreateAsync(User user);

    /// <summary>
    /// Validates and saves changes of an existing account. Active sessions pick up new permissions at their next
    /// periodic validation (at most one minute later).
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(User user);

    /// <summary>
    /// Removes the password and ends every sign-in: the account waits for activation again (the owner proves the e-mail
    /// and chooses a new password; an enrolled authenticator stays and is asked for).
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The result.</returns>
    Task<Result> RemovePasswordAsync(User user);

    /// <summary>
    /// Changes the password after verifying the current one.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>The result.</returns>
    Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword);

    /// <summary>
    /// Disables two-factor authentication and resets the authenticator key; the user re-enrolls at the next sign-in.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The result.</returns>
    Task<Result> ResetAuthenticatorAsync(User user);

    /// <summary>
    /// Changes the security stamp: every cookie and interactive circuit issued before stops working at its next validation
    /// (at most one minute later), independently of the server-side session rows.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>A task that completes when the stamp is changed.</returns>
    Task InvalidateSessionsAsync(User user);

    /// <summary>
    /// Verifies a code of the user's authenticator app (step-up confirmation of sensitive actions).
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="code">The 6-digit code.</param>
    /// <returns><see langword="true"/> when the code is valid and was not used before.</returns>
    Task<bool> VerifyAuthenticatorCodeAsync(User user, string code);

    /// <summary>
    /// Generates a new set of recovery codes.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The recovery codes.</returns>
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(User user);

    /// <summary>
    /// Gets the number of unused recovery codes.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The count.</returns>
    Task<int> CountRecoveryCodesAsync(User user);

    /// <summary>
    /// Removes a lockout.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>A task that completes when the lockout is removed.</returns>
    Task UnlockAsync(User user);
}
