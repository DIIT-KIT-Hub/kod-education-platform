using KOD.Domain.Entities.Auth;

namespace KOD.Domain.Repositories;

/// <summary>
/// Defines methods for managing authentication-related data, such as refresh tokens, in persistence storage.
/// </summary>
public interface IAuthRepository
{
    #region Public methods

    /// <summary>
    /// Retrieves details of a refresh token by its value.
    /// </summary>
    /// <param name="token">The string value of the refresh token to look up.</param>
    /// <returns>
    /// A <see cref="RefreshTokenDetails"/> object if the token exists; otherwise, <c>null</c>.
    /// </returns>
    Task<RefreshTokenDetails?> GetRefreshTokenDetailsByValueAsync(string token);

    /// <summary>
    /// Updates the specified refresh token in the persistence store.
    /// </summary>
    /// <param name="refreshToken">The <see cref="RefreshToken"/> entity containing updated information.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);

    /// <summary>
    /// Deletes a refresh token from the persistence store using its value.
    /// </summary>
    /// <param name="token">The string value of the refresh token to delete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteRefreshTokenByValueAsync(string token);

    #endregion
}
