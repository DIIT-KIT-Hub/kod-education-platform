namespace KOD.Application.DTOs.Tokens;

/// <summary>
/// Data transfer object representing an access token and its expiration time.
/// </summary>
/// <param name="Token">The access token value.</param>
/// <param name="ExpiresAt">The expiration date and time of the access token.</param>
public sealed record AccessTokenDto(string Token, DateTime ExpiresAt);
