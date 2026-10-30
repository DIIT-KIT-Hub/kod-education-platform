using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// Issues and validates the cabinet's JWTs: compact JWS (RFC 7515/7519) signed with ES256 and explicitly typed, so the
/// two kinds can never stand in for each other (RFC 8725 §3.11–3.12):
/// <list type="bullet">
/// <item><c>at+jwt</c> — the access token (RFC 9068 claims: <c>iss</c>, <c>aud</c>, <c>sub</c>, <c>iat</c>, <c>exp</c>,
/// <c>jti</c>) with the sign-in (<c>sid</c>, <c>auth_time</c>, <c>amr</c>), the user's names and effective permissions;
/// a valid one authenticates a request on its own.</item>
/// <item><c>2fa+jwt</c> — the sign-in between the password and the second factor.</item>
/// </list>
/// Validation fixes the algorithm, type, issuer, audience, key (<c>kid</c>) and lifetime on the server (RFC 8725 §3.1–3.10).
/// Refresh tokens are not JWTs: they are opaque, rotated and checked against the server (<see cref="RefreshTokenStore"/>).
/// </summary>
public sealed class JwtTokenService
{
    /// <summary>
    /// The largest accepted token (the default of the handler is 250 KB; ours are about 2 KB).
    /// </summary>
    private const int MaximumTokenSize = 16 * 1024;

    /// <summary>
    /// Claims never copied from a principal: registered claims are set here, sign-in facts come from <see cref="SignInContext"/>.
    /// </summary>
    private static readonly HashSet<string> ReservedClaimTypes =
    [
        JwtRegisteredClaimNames.Iss,
        JwtRegisteredClaimNames.Aud,
        JwtRegisteredClaimNames.Exp,
        JwtRegisteredClaimNames.Nbf,
        JwtRegisteredClaimNames.Iat,
        JwtRegisteredClaimNames.Jti,
        JwtRegisteredClaimNames.Typ,
        AppClaimTypes.SessionId,
        AppClaimTypes.AuthenticationMethod,
        AppClaimTypes.AuthenticationTime,
        AppClaimTypes.Persistent,
    ];

    /// <summary>
    /// The token handler (thread-safe; caches the signature providers of the key).
    /// </summary>
    private readonly JsonWebTokenHandler _handler = new()
    {
        SetDefaultTimesOnTokenCreation = false,
        MaximumTokenSizeInBytes = MaximumTokenSize,
    };

    /// <summary>
    /// The signing key.
    /// </summary>
    private readonly JwtSigningKey _key;

    /// <summary>
    /// The token options.
    /// </summary>
    private readonly JwtOptions _options;

    /// <summary>
    /// The claim type of the security stamp, which must not leave the server.
    /// </summary>
    private readonly string _securityStampClaimType;

    /// <summary>
    /// The validation rules of access tokens.
    /// </summary>
    private readonly TokenValidationParameters _accessRules;

    /// <summary>
    /// The validation rules of second-factor tokens.
    /// </summary>
    private readonly TokenValidationParameters _twoFactorRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="key">The signing key.</param>
    /// <param name="options">The token options.</param>
    /// <param name="identityOptions">The Identity options (claim types).</param>
    public JwtTokenService(JwtSigningKey key, IOptions<JwtOptions> options, IOptions<IdentityOptions> identityOptions)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(identityOptions);

        _key = key;
        _options = options.Value;
        _securityStampClaimType = identityOptions.Value.ClaimsIdentity.SecurityStampClaimType;
        _accessRules = Rules(JwtDefaults.AccessTokenType, _options.Audience, JwtDefaults.AuthenticationScheme, AppClaimTypes.Name);
        _twoFactorRules = Rules(JwtDefaults.TwoFactorTokenType, TwoFactorAudience, IdentityConstants.TwoFactorUserIdScheme, ClaimTypes.Name);
    }

    /// <summary>
    /// Gets the audience of second-factor tokens.
    /// </summary>
    private string TwoFactorAudience => _options.Audience + ":2fa";

    /// <summary>
    /// Reads the sign-in facts from an access token identity.
    /// </summary>
    /// <param name="identity">The identity of an access token.</param>
    /// <returns>The sign-in, or <see langword="null"/> when the identity has no sign-in.</returns>
    public static SignInContext? ReadSignIn(ClaimsIdentity? identity)
    {
        if (identity is null
            || !Guid.TryParse(identity.FindFirst(AppClaimTypes.SessionId)?.Value, out var id)
            || !long.TryParse(identity.FindFirst(AppClaimTypes.AuthenticationTime)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            return null;
        }

        return new SignInContext(
            id,
            DateTimeOffset.FromUnixTimeSeconds(seconds),
            identity.FindFirst(AppClaimTypes.AuthenticationMethod)?.Value,
            identity.HasClaim(AppClaimTypes.Persistent, "true"));
    }

    /// <summary>
    /// Issues an access token for the claims of a user.
    /// </summary>
    /// <param name="userClaims">The user's claims from the Identity claims factory.</param>
    /// <param name="signIn">The sign-in the token belongs to.</param>
    /// <param name="notAfter">The end of the sign-in (the expiry of its refresh token): the token never outlives it.</param>
    /// <param name="now">The current moment.</param>
    /// <returns>The token and the identity it carries.</returns>
    public IssuedAccessToken IssueAccessToken(IEnumerable<Claim> userClaims, SignInContext signIn, DateTimeOffset notAfter, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(userClaims);
        ArgumentNullException.ThrowIfNull(signIn);

        // JWT times have a resolution of one second.
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds());
        var expires = issuedAt + _options.AccessTokenLifetime;
        if (notAfter < expires)
        {
            expires = DateTimeOffset.FromUnixTimeSeconds(notAfter.ToUnixTimeSeconds());
        }

        if (expires <= issuedAt)
        {
            throw new InvalidOperationException("The sign-in has ended.");
        }

        List<Claim> claims =
        [
            .. userClaims.Where(c => !ReservedClaimTypes.Contains(c.Type) && c.Type != _securityStampClaimType).Select(static c => new Claim(c.Type, c.Value)),
            new(AppClaimTypes.SessionId, signIn.Id.ToString()),
            new(AppClaimTypes.AuthenticationTime, signIn.AuthenticatedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
        ];
        if (signIn.AuthenticationMethod is { } method)
        {
            claims.Add(new Claim(AppClaimTypes.AuthenticationMethod, method));
        }

        if (signIn.IsPersistent)
        {
            claims.Add(new Claim(AppClaimTypes.Persistent, "true"));
        }

        if (claims.Find(static c => c.Type == AppClaimTypes.Subject) is null)
        {
            throw new InvalidOperationException("The principal has no user identifier.");
        }

        // The identity of this request must equal the one a validation of the token yields, including the claim issuer:
        // antiforgery tokens are bound to (sub, issuer), so a form rendered now must still validate after the next request.
        return new IssuedAccessToken(
            Create(JwtDefaults.AccessTokenType, _options.Audience, claims, issuedAt, expires),
            expires,
            new ClaimsIdentity(
                claims.Select(c => new Claim(c.Type, c.Value, c.ValueType, _options.Issuer, _options.Issuer)),
                JwtDefaults.AuthenticationScheme,
                AppClaimTypes.Name,
                AppClaimTypes.Role));
    }

    /// <summary>
    /// Issues the token of a sign-in that passed the password and waits for the second factor.
    /// </summary>
    /// <param name="claims">The claims Identity keeps between the steps (the user id, the login provider).</param>
    /// <param name="now">The current moment.</param>
    /// <returns>The token and its expiry.</returns>
    public (string Token, DateTimeOffset ExpiresAt) IssueTwoFactor(IEnumerable<Claim> claims, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds());
        var expires = issuedAt + _options.TwoFactorTokenLifetime;
        var carried = claims.Where(c => !ReservedClaimTypes.Contains(c.Type) && c.Type != _securityStampClaimType).Select(static c => new Claim(c.Type, c.Value));
        return (Create(JwtDefaults.TwoFactorTokenType, TwoFactorAudience, carried, issuedAt, expires), expires);
    }

    /// <summary>
    /// Validates an access token. Never throws for a bad token: the result carries the reason.
    /// </summary>
    /// <param name="token">The compact token.</param>
    /// <returns>The validation result.</returns>
    public Task<TokenValidationResult> ValidateAccessTokenAsync(string token) => _handler.ValidateTokenAsync(token, _accessRules);

    /// <summary>
    /// Validates a second-factor token.
    /// </summary>
    /// <param name="token">The compact token.</param>
    /// <returns>The validation result.</returns>
    public Task<TokenValidationResult> ValidateTwoFactorTokenAsync(string token) => _handler.ValidateTokenAsync(token, _twoFactorRules);

    /// <summary>
    /// Builds the validation rules of one token kind.
    /// </summary>
    /// <param name="type">The required <c>typ</c>.</param>
    /// <param name="audience">The required audience.</param>
    /// <param name="authenticationType">The authentication type of the resulting identity.</param>
    /// <param name="nameClaimType">The name claim type of the resulting identity.</param>
    /// <returns>The rules.</returns>
    private TokenValidationParameters Rules(string type, string audience, string authenticationType, string nameClaimType) => new()
    {
        ValidIssuer = _options.Issuer,
        ValidAudience = audience,
        IssuerSigningKey = _key.SecurityKey,
        ValidAlgorithms = [JwtSigningKey.Algorithm],
        ValidTypes = [type],
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        TryAllIssuerSigningKeys = false,
        ClockSkew = _options.ClockSkew,
        NameClaimType = nameClaimType,
        RoleClaimType = AppClaimTypes.Role,
        AuthenticationType = authenticationType,
    };

    /// <summary>
    /// Creates and signs a token.
    /// </summary>
    /// <param name="type">The <c>typ</c> header.</param>
    /// <param name="audience">The audience.</param>
    /// <param name="claims">The claims.</param>
    /// <param name="issuedAt">The issue moment.</param>
    /// <param name="expires">The expiry.</param>
    /// <returns>The compact token.</returns>
    private string Create(string type, string audience, IEnumerable<Claim> claims, DateTimeOffset issuedAt, DateTimeOffset expires)
        => _handler.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = type,
            Issuer = _options.Issuer,
            Audience = audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(claims),
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N") },
            SigningCredentials = _key.SigningCredentials,
        });
}
