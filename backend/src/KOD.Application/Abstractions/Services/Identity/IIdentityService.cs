using KOD.Application.DTOs.Users;
using KOD.Application.Results;

namespace KOD.Application.Abstractions.Services.Identity;

/// <summary>
/// Defines methods for user identity operations such as confirmation and OTP retrieval.
/// </summary>
public interface IIdentityService
{
    #region Public methods

    /// <summary>
    /// Confirms a user's identity by verifying their email and password.
    /// </summary>
    /// <param name="email">The email address of the user to confirm.</param>
    /// <param name="password">The password provided by the user for verification.</param>
    /// <returns>A result indicating whether the user was successfully verified.</returns>
    Task<Result<bool>> VerifyUserAsync(string email, string password);

    /// <summary>
    /// Retrieves OTP-related details for a user by their email.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>A result containing <see cref="UserOtpDetailsDto"/> with user data required for OTP operations.</returns>
    Task<Result<UserOtpDetailsDto>> GetUserOtpDetailsByEmailAsync(string email);

    /// <summary>
    /// Checks whether a user exists by email.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>A result indicating whether the user exists.</returns>
    Task<Result<bool>> CheckUserExistenceByEmailAsync(string email);

    /// <summary>
    /// Checks whether a user is already verified.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>A result indicating whether the user is verified.</returns>
    Task<Result<bool>> CheckUserVerificationByEmailAsync(string email);

    #endregion
}

