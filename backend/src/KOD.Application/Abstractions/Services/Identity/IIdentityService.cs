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
    /// <returns>A <see cref="Task"/> representing the asynchronous confirmation operation.</returns>
    /// <exception cref="KOD.Domain.Exceptions.Auth.CredentialsException">
    /// Thrown when the provided credentials are invalid.
    /// </exception>
    Task<Result<bool>> VerifyUserAsync(string email, string password);

    /// <summary>
    /// Retrieves OTP-related details for a user by their email.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>
    /// A <see cref="UserOtpDetailsDto"/> containing the user's ID, email, and verification status.
    /// </returns>
    /// <exception cref="KOD.Domain.Exceptions.Users.UserVerifiedException">
    /// Thrown if the user is not verified or OTP cannot be generated.
    /// </exception>
    Task<Result<UserOtpDetailsDto>> GetUserOtpDetailsByEmailAsync(string email);

    Task<Result<bool>> CheckUserExistenceByEmailAsync(string email);

    Task<Result<bool>> CheckUserVerificationByEmailAsync(string email);

    #endregion
}

