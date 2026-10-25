using System.Security.Claims;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Web.Identity;

/// <summary>
/// Re-validates the user of an interactive circuit every minute. The circuit loses its authentication when the account
/// no longer exists, is blocked, has a new security stamp, or when its server-side session was revoked or expired.
/// When the user's permissions or profile claims changed, the circuit gets a fresh principal without a reconnect.
/// </summary>
/// <param name="loggerFactory">The logger factory.</param>
/// <param name="scopeFactory">Creates scopes to read fresh user data.</param>
/// <param name="sessions">Checks the server-side session.</param>
/// <param name="options">The Identity options.</param>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IUserSessionValidator sessions,
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
    private static readonly string[] SignInClaimTypes = [AppClaimTypes.SessionId, AppClaimTypes.AuthenticationMethod];

    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);
        var principal = authenticationState.User;

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
        if (principalStamp != await userManager.GetSecurityStampAsync(user))
        {
            return false;
        }

        if (!Guid.TryParse(principal.FindFirstValue(AppClaimTypes.SessionId), out var sessionId)
            || !await sessions.IsAliveAsync(sessionId, user.Id, cancellationToken))
        {
            return false;
        }

        var factory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
        var fresh = await factory.CreateAsync(user);
        if (!SameUserClaims(principal, fresh))
        {
            CarryOverSignInClaims(principal, fresh);
            SetAuthenticationState(Task.FromResult(new AuthenticationState(fresh)));
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

    /// <summary>
    /// Copies the session and authentication method claims to the refreshed principal.
    /// </summary>
    /// <param name="current">The circuit principal.</param>
    /// <param name="fresh">The refreshed principal.</param>
    private static void CarryOverSignInClaims(ClaimsPrincipal current, ClaimsPrincipal fresh)
    {
        if (fresh.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var type in SignInClaimTypes)
        {
            identity.AddClaims(current.FindAll(type).Select(static c => new Claim(c.Type, c.Value)));
        }
    }
}
