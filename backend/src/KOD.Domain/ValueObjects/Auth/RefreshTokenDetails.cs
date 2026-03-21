using KOD.Domain.ValueObjects.Users;

namespace KOD.Domain.ValueObjects.Auth;

/// <summary>
/// Represents the details of a refresh token, including its value, expiration time, and associated user identity.
/// </summary>
public sealed record RefreshTokenDetails(string Token, DateTime ExpiresAt, UserIdentity User);
