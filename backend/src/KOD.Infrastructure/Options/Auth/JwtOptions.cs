namespace KOD.Infrastructure.Options.Auth;

/// <summary>
/// Represents configuration options for JWT tokens, including keys, issuer, audience, and expiration settings.
/// </summary>
internal sealed class JwtOptions
{
    #region Public fields

    /// <summary>
    /// Gets or sets the key used to sign access tokens.
    /// </summary>
    public string AccessTokenKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer of the tokens.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience for the tokens.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lifetime of access tokens in minutes.
    /// </summary>
    public int AccessTokenMinutes { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of verification tokens in minutes.
    /// </summary>
    public int VerificationTokenMinutes { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of refresh tokens in days.
    /// </summary>
    public int RefreshTokenDays { get; set; }

    #endregion
}
