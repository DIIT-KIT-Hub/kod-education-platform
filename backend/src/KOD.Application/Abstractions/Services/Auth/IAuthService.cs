using KOD.Application.DTOs.Auth;
using KOD.Application.DTOs.Tokens;

namespace KOD.Application.Abstractions.Services.Auth;

/// <summary>
/// Defines methods for authentication operations such as login, token refresh, and logout.
/// </summary>
public interface IAuthService
{
    #region Public methods

    /// <summary>
    /// Authenticates a user with the provided credentials and returns tokens.
    /// </summary>
    /// <param name="request">The login request containing the user's credentials.</param>
    /// <returns>
    /// A <see cref="TokenResponseDto"/> containing the access token and refresh token if authentication succeeds.
    /// </returns>
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request containing the current refresh token.</param>
    /// <returns>
    /// A <see cref="TokenResponseDto"/> containing a new access token and optionally a new refresh token.
    /// </returns>
    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Logs out a user by invalidating the specified refresh token.
    /// </summary>
    /// <param name="request">The refresh token request containing the token to invalidate.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous logout operation.</returns>
    Task LogoutAsync(RefreshTokenRequestDto request);

    #endregion
}
