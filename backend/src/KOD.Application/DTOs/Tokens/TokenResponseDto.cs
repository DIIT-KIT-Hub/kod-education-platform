namespace KOD.Application.DTOs.Tokens;

/// <summary>
/// Data transfer object representing an access and refresh token pair with their expiration times.
/// </summary>
/// <param name="AccessToken">The access token value.</param>
/// <param name="AccessTokenExpiresAt">The expiration date and time of the access token.</param>
/// <param name="RefreshToken">The refresh token value.</param>
/// <param name="RefreshTokenExpiresAt">The expiration date and time of the refresh token.</param>
public sealed record TokenResponseDto(
    string AccessToken, 
    DateTime AccessTokenExpiresAt, 
    string RefreshToken, 
    DateTime RefreshTokenExpiresAt);

