using System.Security.Claims;
using System.Text.Encodings.Web;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KOD.Web.Identity;

/// <summary>
/// Authenticates the cabinet with JWT access tokens and rotating refresh tokens, both kept in <c>__Host-</c> cookies that
/// are <c>HttpOnly</c>, <c>Secure</c> and <c>SameSite=Lax</c> (scripts cannot read them, per the IETF "OAuth 2.0 for
/// Browser-Based Applications"), so server-rendered pages, enhanced navigation, the REST API and Blazor circuits all carry
/// them without any script.
/// <list type="bullet">
/// <item>A valid access token (5 minutes) authenticates the request by its signature alone — no database.</item>
/// <item>When it has expired, the refresh token is rotated (RFC 9700 §4.14.2): the user is read again, so a blocked
/// account or a new security stamp ends the sign-in, and the permissions are fresh. A rotated token that comes back
/// revokes the whole sign-in.</item>
/// <item><see cref="SignInManager{TUser}"/> signs in through this handler; signing out revokes the refresh token family.</item>
/// </list>
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
/// <param name="tokens">The access token service.</param>
/// <param name="refreshTokens">The refresh token store.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="claimsFactory">Builds the user's claims.</param>
/// <param name="identityOptions">The Identity options (claim types).</param>
/// <param name="audit">The audit logger.</param>
internal sealed partial class JwtAuthenticationHandler(
    IOptionsMonitor<JwtAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    JwtTokenService tokens,
    RefreshTokenStore refreshTokens,
    UserManager<User> userManager,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> identityOptions,
    IAuditLogger audit) : SignInAuthenticationHandler<JwtAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>
    /// Marks authentication properties that come from this handler: a sign-in with them continues the same sign-in
    /// (<see cref="SignInManager{TUser}.RefreshSignInAsync"/>) instead of starting a new one.
    /// </summary>
    private const string ContinuationItem = ".kod.continues";

    /// <summary>
    /// Gets a value indicating whether the response can still carry new cookies (not after the response started, and not
    /// on a WebSocket upgrade, whose 101 response browsers may not take cookies from). A refresh token is rotated only
    /// when the browser is certain to receive its successor.
    /// </summary>
    private bool CanIssueCookies => !Response.HasStarted && !Context.WebSockets.IsWebSocketRequest;

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accessToken = Request.Cookies[JwtDefaults.AccessTokenCookie];
        if (!string.IsNullOrEmpty(accessToken))
        {
            var validated = await tokens.ValidateAccessTokenAsync(accessToken);
            if (validated.IsValid && JwtTokenService.ReadSignIn(validated.ClaimsIdentity) is { } signIn)
            {
                return Success(validated.ClaimsIdentity, signIn, new DateTimeOffset(validated.SecurityToken.ValidTo, TimeSpan.Zero));
            }

            if (validated.Exception is not null and not SecurityTokenExpiredException)
            {
                LogTokenRejected(Logger, validated.Exception);
            }
        }

        var refreshToken = Request.Cookies[JwtDefaults.RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken) || !CanIssueCookies)
        {
            return AuthenticateResult.NoResult();
        }

        return await RefreshAsync(refreshToken);
    }

    /// <inheritdoc />
    protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        ArgumentNullException.ThrowIfNull(user);
        properties ??= new AuthenticationProperties();

        var claimTypes = identityOptions.Value.ClaimsIdentity;
        if (!Guid.TryParse(user.FindFirstValue(claimTypes.UserIdClaimType), out var userId)
            || user.FindFirstValue(claimTypes.SecurityStampClaimType) is not { } securityStamp)
        {
            throw new InvalidOperationException("The principal must come from the Identity claims factory (user id and security stamp).");
        }

        // A refresh of the current sign-in (new password, language, photo, signing out elsewhere) keeps the sign-in and
        // binds it to the current security stamp; anything else starts a new sign-in and ends the one this browser had.
        var current = await HandleAuthenticateOnceSafeAsync();
        var currentSignIn = current.Succeeded ? JwtTokenService.ReadSignIn(current.Principal.Identity as ClaimsIdentity) : null;
        var sameUser = current.Succeeded && current.Principal.FindFirstValue(AppClaimTypes.Subject) == userId.ToString();

        RefreshResult? family = null;
        if (properties.Items.ContainsKey(ContinuationItem) && currentSignIn is not null && sameUser)
        {
            family = await refreshTokens.RebindAsync(currentSignIn.Id, userId, securityStamp, Context.RequestAborted);
        }

        if (family is null)
        {
            if (currentSignIn is not null)
            {
                await refreshTokens.RevokeAsync(currentSignIn.Id, Context.RequestAborted);
            }

            family = await refreshTokens.CreateAsync(
                userId,
                securityStamp,
                user.FindFirstValue(AppClaimTypes.AuthenticationMethod),
                properties.IsPersistent,
                Context.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                Context.RequestAborted);
        }

        AppendCookies(family, tokens.IssueAccessToken(user.Claims, family.SignIn!, family.ExpiresAt, TimeProvider.GetUtcNow()));
    }

    /// <inheritdoc />
    protected override async Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        var current = await HandleAuthenticateOnceSafeAsync();
        if (current.Succeeded && JwtTokenService.ReadSignIn(current.Principal.Identity as ClaimsIdentity) is { } signIn)
        {
            await refreshTokens.RevokeAsync(signIn.Id, Context.RequestAborted);
        }
        else if (Request.Cookies[JwtDefaults.RefreshTokenCookie] is { Length: > 0 } refreshToken)
        {
            await refreshTokens.RevokeAsync(refreshToken, Context.RequestAborted);
        }

        DeleteCookies();
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => RedirectOrStatus(properties, Options.LoginPath, StatusCodes.Status401Unauthorized);

    /// <inheritdoc />
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => RedirectOrStatus(properties, Options.AccessDeniedPath, StatusCodes.Status403Forbidden);

    /// <summary>
    /// Builds the attributes of the token cookies.
    /// </summary>
    /// <param name="expires">The expiry, or <see langword="null"/> for a browser session cookie.</param>
    /// <returns>The cookie options.</returns>
    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = expires,
    };

    /// <summary>
    /// Logs an access token rejected for another reason than expiry (a forged or foreign token, a replaced key).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The validation failure.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "A JWT access token was rejected")]
    private static partial void LogTokenRejected(ILogger logger, Exception exception);

    /// <summary>
    /// Rotates the refresh token and issues a new access token.
    /// </summary>
    /// <param name="refreshToken">The refresh token from the cookie.</param>
    /// <returns>The authentication result.</returns>
    private async Task<AuthenticateResult> RefreshAsync(string refreshToken)
    {
        var refreshed = await refreshTokens.RefreshAsync(refreshToken, Context.Connection.RemoteIpAddress?.ToString(), Context.RequestAborted);
        if (refreshed.Status == RefreshStatus.Reused)
        {
            await audit.WriteAsync(AuditActions.SessionRevoked, refreshed.UserId, null, "Повторно використано старий refresh-токен: вхід завершено", Context.RequestAborted);
        }

        if (refreshed.Status != RefreshStatus.Refreshed)
        {
            DeleteCookies();
            return AuthenticateResult.Fail("The refresh token is not valid.");
        }

        // The user is read afresh: a blocked account or a new security stamp ends the sign-in here.
        var user = await userManager.FindByIdAsync(refreshed.UserId!.Value.ToString());
        if (user is null || !user.IsActive || user.SecurityStamp != refreshed.SecurityStamp)
        {
            await refreshTokens.RevokeAsync(refreshed.SignIn!.Id, Context.RequestAborted);
            DeleteCookies();
            return AuthenticateResult.Fail("The sign-in is no longer valid.");
        }

        var principal = await claimsFactory.CreateAsync(user);
        var issued = tokens.IssueAccessToken(principal.Claims, refreshed.SignIn!, refreshed.ExpiresAt, TimeProvider.GetUtcNow());
        AppendCookies(refreshed, issued);
        return Success(issued.Identity, refreshed.SignIn!, issued.ExpiresAt);
    }

    /// <summary>
    /// Builds a successful result.
    /// </summary>
    /// <param name="identity">The identity of the access token.</param>
    /// <param name="signIn">The sign-in.</param>
    /// <param name="expires">The expiry of the access token.</param>
    /// <returns>The result.</returns>
    private AuthenticateResult Success(ClaimsIdentity identity, SignInContext signIn, DateTimeOffset expires)
    {
        var properties = new AuthenticationProperties { IsPersistent = signIn.IsPersistent, IssuedUtc = signIn.AuthenticatedAt, ExpiresUtc = expires };
        properties.Items[ContinuationItem] = "1";
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Scheme.Name));
    }

    /// <summary>
    /// Writes the token cookies. A persistent sign-in keeps them until the tokens expire; otherwise they are browser
    /// session cookies, so closing the browser signs out.
    /// </summary>
    /// <param name="family">The refresh token family (its current token, when it changed, and its expiry).</param>
    /// <param name="access">The access token.</param>
    private void AppendCookies(RefreshResult family, IssuedAccessToken access)
    {
        var persistent = family.SignIn!.IsPersistent;
        Response.Cookies.Append(JwtDefaults.AccessTokenCookie, access.Token, CookieOptions(persistent ? access.ExpiresAt : null));
        if (family.Token is { } refreshToken)
        {
            Response.Cookies.Append(JwtDefaults.RefreshTokenCookie, refreshToken, CookieOptions(persistent ? family.ExpiresAt : null));
        }
    }

    /// <summary>
    /// Removes both token cookies.
    /// </summary>
    private void DeleteCookies()
    {
        if (Response.HasStarted)
        {
            return;
        }

        Response.Cookies.Delete(JwtDefaults.AccessTokenCookie, CookieOptions(null));
        Response.Cookies.Delete(JwtDefaults.RefreshTokenCookie, CookieOptions(null));
    }

    /// <summary>
    /// Redirects page requests and answers API requests with a status code.
    /// </summary>
    /// <param name="properties">The authentication properties.</param>
    /// <param name="page">The page to redirect to.</param>
    /// <param name="status">The status code for API requests.</param>
    /// <returns>A completed task.</returns>
    private Task RedirectOrStatus(AuthenticationProperties properties, PathString page, int status)
    {
        if (Request.Path.StartsWithSegments(Options.ApiPath, StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = status;
            return Task.CompletedTask;
        }

        var returnUrl = properties.RedirectUri ?? OriginalPathBase + OriginalPath + Request.QueryString;
        Response.Redirect(BuildRedirectUri(page + QueryString.Create(Options.ReturnUrlParameter, returnUrl)));
        return Task.CompletedTask;
    }
}
