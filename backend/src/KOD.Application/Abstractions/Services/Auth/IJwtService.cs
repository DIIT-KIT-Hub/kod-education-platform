using KOD.Application.DTOs.Tokens;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Abstractions.Services.Auth;

/// <summary>
/// Defines methods for generating JWT access and refresh tokens.
/// </summary>
public interface IJwtService
{
    #region Public methods

    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="userLoginDetails">The user's login details used to populate claims in the access token.</param>
    /// <returns>An <see cref="AccessTokenDto"/> containing the generated JWT access token and its expiration time.</returns>
    AccessTokenDto GenerateAccessToken(UserLoginDetails userLoginDetails);

    /// <summary>
    /// Generates a verification token with a fixed "Verification" role.
    /// </summary>
    /// <returns>An <see cref="AccessTokenDto"/> containing the verification JWT and its expiration time.</returns>
    AccessTokenDto GenerateVerificationToken();

    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <returns>A <see cref="RefreshTokenDto"/> containing the refresh token string and its expiration time.</returns>
    RefreshTokenDto GenerateRefreshToken();

    #endregion
}
