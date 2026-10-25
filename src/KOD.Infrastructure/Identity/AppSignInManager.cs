using KOD.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Sign-in manager with two cabinet-specific rules:
/// blocked accounts cannot sign in (or keep their sessions), and two-factor authentication is mandatory —
/// after the password step every user is routed to the second factor, where users without an authenticator enroll one.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="contextAccessor">The HTTP context accessor.</param>
/// <param name="claimsFactory">The claims factory.</param>
/// <param name="optionsAccessor">The Identity options.</param>
/// <param name="logger">The logger.</param>
/// <param name="schemes">The authentication schemes.</param>
/// <param name="confirmation">The account confirmation policy.</param>
internal sealed class AppSignInManager(
    UserManager<User> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<User>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation)
    : SignInManager<User>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    /// <inheritdoc />
    public override async Task<bool> CanSignInAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.IsActive && await base.CanSignInAsync(user);
    }

    /// <inheritdoc />
    public override Task<bool> IsTwoFactorEnabledAsync(User user) => Task.FromResult(true);
}
