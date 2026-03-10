using KOD.Application.DTOs.Tokens;
using KOD.Domain.Entities.Auth;

namespace KOD.Application.Mappings;

/// <summary>
/// Provides extension methods for mapping token-related DTOs to domain entities.
/// </summary>
public static class TokensMappingExtensions
{
    #region Public methods

    /// <summary>
    /// Converts a <see cref="RefreshTokenDto"/> into a <see cref="RefreshToken"/> entity.
    /// </summary>
    /// <param name="refreshTokenDto">The data transfer object containing refresh token information.</param>
    /// <param name="userId">The unique identifier of the user who owns the refresh token.</param>
    /// <returns>
    /// A new <see cref="RefreshToken"/> entity populated with values from the DTO and the specified user ID.
    /// </returns>
    public static RefreshToken ToEntity(this RefreshTokenDto refreshTokenDto, Guid userId)
        => new RefreshToken()
        {
            Token = refreshTokenDto.Token,
            ExpiresAt = refreshTokenDto.ExpiresAt,
            UserId = userId,
        };

    #endregion
}
