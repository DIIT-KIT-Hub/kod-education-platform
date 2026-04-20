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
    /// <returns>A result indicating whether the OTP code was successfully sent.</returns>
    Task<Result<DateTime>> SendOtpCodeAsync(UserOtpDetailsDto user);

    /// <summary>
    /// Checks whether the provided OTP code is valid for the specified user.
    /// </summary>
    /// <param name="otpCode">The OTP code provided by the user.</param>
    /// <param name="user">The user details to validate the OTP against.</param>
    /// <returns>A result indicating whether the OTP code is valid.</returns>
    Task<Result<bool>> CheckOtpCodeAsync(string otpCode, UserOtpDetailsDto user);

    #endregion
}
