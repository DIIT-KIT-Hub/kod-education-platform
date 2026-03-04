using KOD.Application.DTOs.Otp;
using KOD.Application.Result;

namespace KOD.Application.Abstractions.Persitence.Repositories.Otp;

/// <summary>
/// Defines methods for managing OTP codes in persistence storage.
/// </summary>
public interface IOtpRepository
{
    #region Public methods

    /// <summary>
    /// Retrieves the OTP code for a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the OTP code data.</returns>
    Task<ApiResult<OtpCodeDto>> GetOtpCodeByUserIdAsync(Guid userId);

    /// <summary>
    /// Deletes all the OTP code associated with a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the deletion was successful.</returns>
    Task<ApiResult<bool>> DeleteAllOtpCodesByUserIdAsync(Guid userId);

    /// <summary>
    /// Deletes the OTP code associated with a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the deletion was successful.</returns>
    Task<ApiResult<bool>> DeleteOtpCodeByUserIdAsync(Guid userId);

    /// <summary>
    /// Adds a new OTP code for a user.
    /// </summary>
    /// <param name="otpCode">The OTP code to add.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the addition was successful.</returns>
    Task<ApiResult<bool>> AddOtpCodeAsync(string otpCode, Guid userId);

    #endregion
}
