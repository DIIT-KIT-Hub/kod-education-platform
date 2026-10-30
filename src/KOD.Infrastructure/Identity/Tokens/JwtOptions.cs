namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// JWT settings (section <c>Authentication:Jwt</c>). How long a sign-in lasts (the refresh token) is configured in
/// <see cref="SessionLifetimeOptions"/>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>
    /// Gets or sets the issuer (<c>iss</c>) written into and required from every token.
    /// </summary>
    public string Issuer { get; set; } = "urn:kod";

    /// <summary>
    /// Gets or sets the audience (<c>aud</c>) of access tokens. Refresh and second-factor tokens get their own audiences
    /// derived from it, so no token is accepted for another purpose (RFC 8725 §3.12).
    /// </summary>
    public string Audience { get; set; } = "urn:kod:web";

    /// <summary>
    /// Gets or sets the lifetime of an access token. Requests with a valid access token need no database, so this is the
    /// longest time a blocked account, a new password or changed permissions take to reach an already issued token.
    /// Standards give no number (RFC 6819 §3.2: "minutes or hours"; RFC 9700 §4.14 and RFC 7009 §3: short-lived); 5 minutes
    /// is the shortest lifetime Okta and Amazon Cognito allow (their defaults: 1 hour), chosen because browser tokens
    /// cannot be sender-constrained (RFC 9449 DPoP needs a header that page navigations do not send).
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets how long the just-rotated refresh token is still accepted: requests a browser sent in parallel carry
    /// the same token, and the losers of the race get the current token instead of revoking the family (the "reuse
    /// interval" / "grace period" of Auth0 and Okta; 0 makes reuse detection strict). Within it nothing is rotated.
    /// </summary>
    public TimeSpan RefreshTokenReuseInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the lifetime of the token that remembers the passed password step until the second factor.
    /// </summary>
    public TimeSpan TwoFactorTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the tolerated clock difference when checking token lifetimes.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the PEM file with the ES256 (ECDSA P-256) private key that signs the tokens (PKCS#8, optionally
    /// encrypted, or SEC1). When empty, a key is generated on first start and kept in <c>App_Data/keys</c> encrypted with
    /// Data Protection; set a file when several servers must share the key. Replacing the key signs nobody out: an access
    /// token with an unknown signature is simply replaced from the refresh token, which does not depend on this key.
    /// </summary>
    public string? SigningKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the password of an encrypted PKCS#8 signing key.
    /// </summary>
    public string? SigningKeyPassword { get; set; }
}
