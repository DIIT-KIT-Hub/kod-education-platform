namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// Names shared by the token service and the authentication handlers.
/// </summary>
public static class JwtDefaults
{
    /// <summary>
    /// The authentication scheme of signed-in users (JWT access token + JWT refresh token in cookies).
    /// </summary>
    public const string AuthenticationScheme = "Jwt";

    /// <summary>
    /// The cookie with the access token. <c>__Host-</c>: only over HTTPS, for this host and path <c>/</c>.
    /// </summary>
    public const string AccessTokenCookie = "__Host-kod.at";

    /// <summary>
    /// The cookie with the opaque, rotating refresh token.
    /// </summary>
    public const string RefreshTokenCookie = "__Host-kod.rt";

    /// <summary>
    /// The cookie with the token of a sign-in waiting for the second factor.
    /// </summary>
    public const string TwoFactorCookie = "__Host-kod.2fa";

    /// <summary>
    /// The JOSE <c>typ</c> of access tokens (RFC 9068 §2.1; explicit typing per RFC 8725 §3.11).
    /// </summary>
    public const string AccessTokenType = "at+jwt";

    /// <summary>
    /// The JOSE <c>typ</c> of the token that carries a sign-in between the password and the second factor.
    /// </summary>
    public const string TwoFactorTokenType = "2fa+jwt";
}
