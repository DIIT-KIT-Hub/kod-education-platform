using KOD.Application.DTOs.Auth;
using KOD.Application.Result;

namespace KOD.Application.Abstractions.Services.Auth;

/// <summary>
/// Defines methods for authentication operations such as login, token refresh, and logout.
/// </summary>
public interface IAuthService
{
    #region Public methods

    /// <summary>
    /// Logs in a user with the specified credentials.
    /// </summary>
    /// <param name="request">The login request containing username and password.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the access and refresh tokens.</returns>
    Task<ApiResult<TokenResponseDto>> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Refreshes an access token using the provided refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the new access and refresh tokens.</returns>
    Task<ApiResult<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Logs out a user by invalidating the specified refresh token.
    /// </summary>
    /// <param name="request">The refresh token request to be invalidated.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating success or failure of the logout operation.</returns>
    Task<ApiResult<bool>> LogoutAsync(RefreshTokenRequestDto request);

    #endregion
}
