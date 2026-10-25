using System.Security.Claims;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Web.Identity;

/// <summary>
/// Re-validates the user of an interactive circuit every minute: the account must still exist, be active
/// and have the same security stamp; otherwise the circuit loses its authentication.
/// </summary>
/// <param name="loggerFactory">The logger factory.</param>
/// <param name="scopeFactory">Creates scopes to read fresh user data.</param>
/// <param name="options">The Identity options.</param>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> options) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.GetUserAsync(authenticationState.User);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var principalStamp = authenticationState.User.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
        return principalStamp == await userManager.GetSecurityStampAsync(user);
    }
}
