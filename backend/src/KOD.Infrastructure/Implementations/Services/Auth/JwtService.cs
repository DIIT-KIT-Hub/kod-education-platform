using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.DTOs.Auth;
using KOD.Application.Result;
using KOD.Domain.Entities.Auth;
using KOD.Domain.Entities.Users;
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
    public ApiResult<AccessTokenDto> GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.AccessTokenKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return ApiResult<AccessTokenDto>
            .Success(new AccessTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expires));
    }

    /// <inheritdoc />
    public ApiResult<RefreshToken> GenerateRefreshToken(ApplicationUser user)
    {
        var refreshTokenString = GenerateRefreshTokenString();

        return ApiResult<RefreshToken>.Success(new RefreshToken()
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenString,
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });
    }

    #endregion

    #region Private methods

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
