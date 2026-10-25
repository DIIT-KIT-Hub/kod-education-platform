namespace KOD.Application.Features.Profile;

/// <summary>
/// An active sign-in session of the current user.
/// </summary>
/// <param name="Id">The session identifier.</param>
/// <param name="CreatedAt">The sign-in moment.</param>
/// <param name="LastSeenAt">The last activity moment.</param>
/// <param name="ExpiresAt">The expiration moment.</param>
/// <param name="IpAddress">The client IP address.</param>
/// <param name="UserAgent">The client user agent.</param>
/// <param name="IsCurrent">Whether this is the session of the current request.</param>
public sealed record SessionDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? ExpiresAt,
    string? IpAddress,
    string? UserAgent,
    bool IsCurrent);
