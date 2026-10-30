using System.Security.Claims;
using System.Text.Encodings.Web;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Web.Identity;

/// <summary>
/// Keeps a sign-in that passed the password step until the second factor is entered, as a short-lived <c>2fa+jwt</c>
/// token in a <c>SameSite=Strict</c> cookie. Registered under <see cref="IdentityConstants.TwoFactorUserIdScheme"/>, the
/// scheme <see cref="SignInManager{TUser}"/> uses between the two steps.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
/// <param name="tokens">The token service.</param>
internal sealed class TwoFactorJwtHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    JwtTokenService tokens) : SignInAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Cookies[JwtDefaults.TwoFactorCookie] is not { Length: > 0 } token)
        {
            return AuthenticateResult.NoResult();
        }

        var validated = await tokens.ValidateTwoFactorTokenAsync(token);
        if (!validated.IsValid)
        {
            DeleteCookie();
            return AuthenticateResult.Fail("The second-factor token is not valid.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(validated.ClaimsIdentity), Scheme.Name));
    }

    /// <inheritdoc />
    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        ArgumentNullException.ThrowIfNull(user);
        var (token, expires) = tokens.IssueTwoFactor(user.Claims, TimeProvider.GetUtcNow());
        Response.Cookies.Append(JwtDefaults.TwoFactorCookie, token, CookieOptions(expires));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        DeleteCookie();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds the attributes of the cookie: it is sent only by the cabinet's own pages.
    /// </summary>
    /// <param name="expires">The expiry.</param>
    /// <returns>The cookie options.</returns>
    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        Expires = expires,
    };

    /// <summary>
    /// Removes the cookie.
    /// </summary>
    private void DeleteCookie()
    {
        if (!Response.HasStarted)
        {
            Response.Cookies.Delete(JwtDefaults.TwoFactorCookie, CookieOptions(null));
        }
    }
}
