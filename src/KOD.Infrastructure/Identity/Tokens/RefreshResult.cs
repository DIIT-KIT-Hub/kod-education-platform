namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// The result of presenting a refresh token to <see cref="RefreshTokenStore.RefreshAsync"/>.
/// </summary>
/// <param name="Status">The outcome.</param>
/// <param name="UserId">The user of the family, when the family is known.</param>
/// <param name="SignIn">The sign-in of the family (when refreshed).</param>
/// <param name="SecurityStamp">The security stamp the family is bound to (when refreshed).</param>
/// <param name="Token">The refresh token the browser must keep from now on (when refreshed).</param>
/// <param name="ExpiresAt">The expiry of that token (when refreshed).</param>
public sealed record RefreshResult(
    RefreshStatus Status,
    Guid? UserId = null,
    SignInContext? SignIn = null,
    string? SecurityStamp = null,
    string? Token = null,
    DateTimeOffset ExpiresAt = default);
