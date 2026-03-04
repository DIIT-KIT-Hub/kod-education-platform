using KOD.Application.Result;
using KOD.Domain.Entities.Auth;

namespace KOD.Application.Abstractions.Persitence.Repositories.Auth;

/// <summary>
/// Defines methods for managing authentication-related data, such as refresh tokens, in persistence storage.
/// </summary>
public interface IAuthRepository
{
    #region Public methods

    /// <summary>
    /// Retrieves a refresh token by its value.
    /// </summary>
    /// <param name="token">The value of the refresh token.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the refresh token.</returns>
    Task<ApiResult<RefreshToken>> GetRefreshTokenByValueAsync(string token);

    /// <summary>
    /// Updates an existing refresh token for a user.
    /// </summary>
    /// <param name="refreshToken">The refresh token to update.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the update was successful.</returns>
    Task<ApiResult<bool>> UpdateUserRefreshTokenAsync(RefreshToken refreshToken);

    /// <summary>
    /// Deletes a refresh token by its value.
    /// </summary>
    /// <param name="token">The value of the refresh token to delete.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating whether the deletion was successful.</returns>
    Task<ApiResult<bool>> DeleteRefreshTokenAsync(string token);

    #endregion
}
