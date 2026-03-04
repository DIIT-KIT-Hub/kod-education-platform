using KOD.Application.DTOs.Otp;
using KOD.Application.Result;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Abstractions.Persitence.Repositories.Identity;

/// <summary>
/// Defines methods for accessing and managing user identity data in persistence storage.
/// </summary>
public interface IIdentityRepository
{
    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the user.</returns>
    Task<ApiResult<ApplicationUser>> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Confirms a user's password and updates their record as confirmed.
    /// </summary>
    /// <param name="user">The user to confirm.</param>
    /// <param name="password">The password to verify.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating success or failure.</returns>
    Task<ApiResult<bool>> ConfirmUserAsync(ApplicationUser user, string password);

    /// <summary>
    /// Retrieves a user for OTP operations by their username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the user's OTP data.</returns>
    Task<ApiResult<UserOtpDto>> GetUserForOtpAsync(string username);

    /// <summary>
    /// Retrieves the roles assigned to a user.
    /// </summary>
    /// <param name="user">The user whose roles are retrieved.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing a collection of role names.</returns>
    Task<ApiResult<IEnumerable<string>>> GetUserRolesAsync(ApplicationUser user);

    /// <summary>
    /// Checks if the provided password matches the user's stored password.
    /// </summary>
    /// <param name="user">The user whose password is checked.</param>
    /// <param name="password">The password to verify.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the password is correct.</returns>
    Task<ApiResult<bool>> CheckUserPasswordAsync(ApplicationUser user, string password);
}
