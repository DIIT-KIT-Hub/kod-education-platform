using KOD.Application.DTOs.Users;
using KOD.Application.Results;

namespace KOD.Application.Abstractions.Services.Otp;

/// <summary>
/// Defines methods for requesting and verifying OTP codes for users.
/// </summary>
public interface IOtpService
{
    #region Public methods

    /// <summary>
    /// Requests a new OTP code for the specified user.
    /// </summary>
    /// <param name="user">The user details for whom the OTP code should be generated.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="KOD.Application.Exceptions.Users.UserVerifiedException">
    /// Thrown if the user is already verified and does not require an OTP.
    /// </exception>
    Task<Result<bool>> SendOtpCodeAsync(UserOtpDetailsDto user);

    /// <summary>
    /// Checks whether the provided OTP code is valid for the specified user.
    /// </summary>
    /// <param name="otpCode">The OTP code provided by the user.</param>
    /// <param name="user">The user details to validate the OTP against.</param>
    /// <returns>
    /// A <see cref="Task{Boolean}"/> that returns <c>true</c> if the OTP code is valid; otherwise, <c>false</c>.
    /// </returns>
    Task<Result<bool>> CheckOtpCodeAsync(string otpCode, UserOtpDetailsDto user);

    #endregion
}
