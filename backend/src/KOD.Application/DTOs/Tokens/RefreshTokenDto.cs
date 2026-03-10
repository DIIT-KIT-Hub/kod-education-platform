namespace KOD.Application.DTOs.Tokens;

/// <summary>
/// Data transfer object that represents a refresh token issued to a client.
/// Contains the token value and its expiration time.
/// </summary>
/// <param name="Token">The refresh token string used to obtain a new access token.</param>
/// <param name="ExpiresAt">The UTC date and time when the refresh token expires.</param>
public sealed record RefreshTokenDto(string Token, DateTime ExpiresAt);
