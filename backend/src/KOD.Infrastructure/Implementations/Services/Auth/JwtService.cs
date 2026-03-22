using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.DTOs.Tokens;
using KOD.Domain.ValueObjects.Users;
using KOD.Infrastructure.Options.Auth;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KOD.Infrastructure.Implementations.Services.Auth;

/// <summary>
/// Implements <see cref="IJwtService"/> for generating JWT access and refresh tokens.
/// </summary>
internal sealed class JwtService : IJwtService
{
    #region Private fields

    /// <summary>
    /// The JWT configuration options.
    /// </summary>
    private readonly JwtOptions _jwtOptions;

    /// <summary>
    /// Roles assigned to verification tokens (used when user ID is not present).
    /// </summary>
    private readonly string[] _verificationRoles = ["Verification"];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class with the specified JWT options.
    /// </summary>
    /// <param name="jwtOptions">The JWT configuration options.</param>
    public JwtService(IOptions<JwtOptions> jwtOptions) => _jwtOptions = jwtOptions.Value;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public AccessTokenDto GenerateAccessToken(UserLoginDetails userLoginDetails)
        => GenerateToken(userLoginDetails.Id, userLoginDetails.Roles, _jwtOptions.AccessTokenMinutes);

    /// <inheritdoc />
    public AccessTokenDto GenerateVerificationToken() 
        => GenerateToken(Guid.Empty, _verificationRoles, _jwtOptions.VerificationTokenMinutes);

    /// <inheritdoc />
    public RefreshTokenDto GenerateRefreshToken()
    {
        var refreshTokenString = GenerateRefreshTokenString();

        return new RefreshTokenDto(refreshTokenString, DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Generates a JWT access token with the specified user ID, roles, and expiration time.
    /// </summary>
    /// <param name="userId">The user's ID (use Guid.Empty for verification tokens).</param>
    /// <param name="roles">The roles to include in the token.</param>
    /// <param name="tokenMinutes">Token lifetime in minutes.</param>
    /// <returns>An <see cref="AccessTokenDto"/> containing the JWT and expiration.</returns>
    private AccessTokenDto GenerateToken(Guid userId, IEnumerable<string> roles, int tokenMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.AccessTokenKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>();
        if (userId != Guid.Empty)
        {
            claims.Add(new(JwtRegisteredClaimNames.Sub, userId.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(tokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new AccessTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>
    /// Generates a cryptographically secure random refresh token string.
    /// </summary>
    /// <returns>A base64-encoded refresh token string.</returns>
    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    #endregion
}
