namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// The outcome of presenting a refresh token.
/// </summary>
public enum RefreshStatus
{
    /// <summary>The token was the current one and has been rotated, or it raced a rotation within the grace period.</summary>
    Refreshed,

    /// <summary>The token is malformed, forged, expired or belongs to an ended sign-in.</summary>
    Invalid,

    /// <summary>An already rotated token came back: the family has been revoked (RFC 9700 §4.14.2).</summary>
    Reused,
}
