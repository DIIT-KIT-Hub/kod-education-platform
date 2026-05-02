using KOD.Domain.Entities.Otp;

namespace KOD.Domain.Repositories;

/// <summary>
/// Defines methods for managing OTP codes in persistence storage.
/// </summary>
public interface IOtpRepository
{
    #region Public methods

    /// <summary>
    /// Retrieves the OTP code details for a specific user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>An <see cref="OtpCodeDetails"/> object if an OTP exists for the user; otherwise, <c>null</c>.</returns>
    Task<OtpCodeDetails?> GetOtpCodeDetailsByUserIdAsync(Guid userId);

    /// <summary>
    /// Deletes the OTP code associated with a specific user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose OTP should be deleted.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteOtpCodeByUserIdAsync(Guid userId);

    /// <summary>
    /// Adds a new OTP code for the specified user.
    /// </summary>
    /// <param name="otpCode">The OTP code to store.</param>
    /// <param name="userId">The unique identifier of the user the OTP belongs to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<DateTime> AddOtpCodeAsync(string otpCode, Guid userId);

    #endregion
}
