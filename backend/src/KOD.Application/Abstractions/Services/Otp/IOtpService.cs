using KOD.Application.DTOs.Otp;
using KOD.Application.Result;

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
    /// <param name="user">The user for whom the OTP code is requested.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating success or failure of the request.</returns>
    Task<ApiResult<bool>> RequestOtpCodeAsync(UserOtpDto user);

    /// <summary>
    /// Checks whether the provided OTP code is valid for the specified user.
    /// </summary>
    /// <param name="otpCode">The OTP code to verify.</param>
    /// <param name="user">The user for whom the OTP code is checked.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the OTP code is valid.</returns>
    Task<ApiResult<bool>> CheckOtpCodeAsync(string otpCode, UserOtpDto user);

    #endregion
}
