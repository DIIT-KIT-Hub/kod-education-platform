using KOD.Domain.Users;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Sign-in manager with three cabinet-specific rules:
/// a sign-in issues a JWT session (<see cref="JwtDefaults.AuthenticationScheme"/>) instead of the Identity cookie,
/// blocked accounts cannot sign in (or keep their sessions), and two-factor authentication is mandatory —
/// after the password step every user is routed to the second factor, where users without an authenticator enroll one.
/// </summary>
internal sealed class AppSignInManager : SignInManager<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppSignInManager"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="contextAccessor">The HTTP context accessor.</param>
    /// <param name="claimsFactory">The claims factory.</param>
    /// <param name="optionsAccessor">The Identity options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="schemes">The authentication schemes.</param>
    /// <param name="confirmation">The account confirmation policy.</param>
    public AppSignInManager(
        UserManager<User> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<User>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<User> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        AuthenticationScheme = JwtDefaults.AuthenticationScheme;
    }

    /// <inheritdoc />
    public override async Task<bool> CanSignInAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.IsActive && await base.CanSignInAsync(user);
    }

    /// <inheritdoc />
    public override Task<bool> IsTwoFactorEnabledAsync(User user) => Task.FromResult(true);
}
