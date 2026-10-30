using System.Security.Claims;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Web.Identity;

/// <summary>
/// Re-validates the user of an interactive circuit every minute: a circuit outlives the access token it started with, so
/// it repeats the checks of a token refresh. The circuit loses its authentication when the account no longer exists or is
/// blocked, or when its sign-in ended: signed out, revoked after a refresh token reuse, expired, or bound to an old
/// security stamp (password change, "sign out everywhere"). Changed permissions or profile claims reach the circuit
/// without a reconnect.
/// </summary>
/// <param name="loggerFactory">The logger factory.</param>
/// <param name="scopeFactory">Creates scopes to read fresh user data.</param>
/// <param name="refreshTokens">Knows whether a sign-in is alive.</param>
/// <param name="options">The Identity options (claim types).</param>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    RefreshTokenStore refreshTokens,
    IOptions<IdentityOptions> options) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    /// <summary>
    /// Claims that are derived from the user record and may change while the circuit is open.
    /// </summary>
    private static readonly string[] UserClaimTypes =
        [AppClaimTypes.Permission, AppClaimTypes.FullName, AppClaimTypes.ShortName, AppClaimTypes.MustChangePassword];

    /// <summary>
    /// Claims that describe the sign-in rather than the user; they are carried over to a refreshed principal.
    /// </summary>
    private static readonly string[] SignInClaimTypes =
        [AppClaimTypes.SessionId, AppClaimTypes.AuthenticationMethod, AppClaimTypes.AuthenticationTime, AppClaimTypes.Persistent];

    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);
        var principal = authenticationState.User;
        if (JwtTokenService.ReadSignIn(principal.Identity as ClaimsIdentity) is not { } signIn)
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive || !await refreshTokens.IsAliveAsync(signIn.Id, user.Id, user.SecurityStamp, cancellationToken))
        {
            return false;
        }

        var factory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
        var fresh = await factory.CreateAsync(user);
        if (!SameUserClaims(principal, fresh))
        {
            // The same claims an access token carries, under the same authentication type, without the security stamp.
            // The claim issuer stays the token's: antiforgery binds its tokens to (sub, issuer).
            var stampType = options.Value.ClaimsIdentity.SecurityStampClaimType;
            var issuer = principal.FindFirst(AppClaimTypes.Subject)?.Issuer ?? ClaimsIdentity.DefaultIssuer;
            var identity = new ClaimsIdentity(
                fresh.Claims.Where(c => c.Type != stampType).Select(c => new Claim(c.Type, c.Value, c.ValueType, issuer, issuer)),
                principal.Identity?.AuthenticationType,
                AppClaimTypes.Name,
                AppClaimTypes.Role);
            identity.AddClaims(SignInClaimTypes.SelectMany(principal.FindAll).Select(c => new Claim(c.Type, c.Value, c.ValueType, issuer, issuer)));
            SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        return true;
    }

    /// <summary>
    /// Compares the user-derived claims of two principals.
    /// </summary>
    /// <param name="current">The circuit principal.</param>
    /// <param name="fresh">The principal built from the database.</param>
    /// <returns><see langword="true"/> when nothing changed.</returns>
    private static bool SameUserClaims(ClaimsPrincipal current, ClaimsPrincipal fresh)
    {
        foreach (var type in UserClaimTypes)
        {
            var before = current.FindAll(type).Select(static c => c.Value).Order(StringComparer.Ordinal);
            var after = fresh.FindAll(type).Select(static c => c.Value).Order(StringComparer.Ordinal);
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
