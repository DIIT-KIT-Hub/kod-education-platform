using KOD.Application.DTOs.Auth;
using KOD.Application.Result;
using KOD.Domain.Entities.Auth;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Abstractions.Services.Auth;

/// <summary>
/// Defines methods for generating JWT access and refresh tokens.
/// </summary>
public interface IJwtService
{
    #region Public methods

    /// <summary>
    /// Generates an access token for the specified user with the given roles.
    /// </summary>
    /// <param name="user">The user for whom the access token is generated.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the access token and its expiration time.</returns>
    ApiResult<AccessTokenDto> GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>
    /// Generates a refresh token for the specified user.
    /// </summary>
    /// <param name="user">The user for whom the refresh token is generated.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the refresh token.</returns>
    ApiResult<RefreshToken> GenerateRefreshToken(ApplicationUser user);

    #endregion
}
