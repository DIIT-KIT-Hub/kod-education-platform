using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Identity;

/// <summary>
/// Adds the user's names, photo, interface language, effective permissions and password-change flag to the principal.
/// The factory runs at sign-in and at every access token refresh, so permission changes propagate with the next token.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="optionsAccessor">The Identity options.</param>
/// <param name="permissions">The permissions of the account in all its organizations (the request narrows them).</param>
/// <param name="httpContextAccessor">The current request (its cancellation token), if any.</param>
internal sealed class AppUserClaimsPrincipalFactory(
    UserManager<User> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    IAccountPermissions permissions,
    IHttpContextAccessor httpContextAccessor) : UserClaimsPrincipalFactory<User>(userManager, optionsAccessor)
{
    /// <inheritdoc />
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = await base.GenerateClaimsAsync(user);

        // The stored user claims are the individual permission adjustments; the principal carries the resolved result.
        foreach (var claim in identity.Claims.Where(static c => c.Type is PermissionClaimTypes.Permission or PermissionClaimTypes.DeniedPermission).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));
        identity.AddClaim(new Claim(AppClaimTypes.ShortName, user.ShortName));
        if (user.Language is { } language)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Language, language));
        }

        if (user.PhotoId is { } photo)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Photo, photo.ToString("N")));
        }

        var held = await permissions.UnionAsync(user.Id, httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
        foreach (var permission in held.Order(StringComparer.Ordinal))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        return identity;
    }
}
