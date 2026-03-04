using KOD.Application.DTOs.Otp;
using KOD.Application.Result;

namespace KOD.Application.Abstractions.Services.Identity;

/// <summary>
/// Defines methods for user identity operations such as confirmation and OTP retrieval.
/// </summary>
public interface IIdentityService
{
    #region Public methods

    /// <summary>
    /// Confirms a user's credentials using their username and password.
    /// </summary>
    /// <param name="username">The username of the user to confirm.</param>
    /// <param name="password">The password of the user to confirm.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating success or failure of the confirmation.</returns>
    Task<ApiResult<bool>> ConfirmUserAsync(string username, string password);

    /// <summary>
    /// Retrieves a user for OTP operations by their username.
    /// </summary>
    /// <param name="username">The username of the user to retrieve.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the user's OTP data.</returns>
    Task<ApiResult<UserOtpDto>> GetUserForOtpAsync(string username);

    #endregion
}

