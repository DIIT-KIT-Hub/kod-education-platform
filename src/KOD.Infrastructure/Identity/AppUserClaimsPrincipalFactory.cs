using System.Security.Claims;
using KOD.Application.Common.Access;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Adds the user's names, effective permissions and password-change flag to the authentication principal.
/// The factory runs at sign-in and at every periodic security stamp validation, so permission changes propagate automatically.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="optionsAccessor">The Identity options.</param>
/// <param name="permissionResolver">The permission resolver.</param>
internal sealed class AppUserClaimsPrincipalFactory(
    UserManager<User> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    IPermissionResolver permissionResolver) : UserClaimsPrincipalFactory<User>(userManager, optionsAccessor)
{
    /// <inheritdoc />
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));
        identity.AddClaim(new Claim(AppClaimTypes.ShortName, user.ShortName));
        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        var permissions = await permissionResolver.GetEffectivePermissionsAsync(user.Id, CancellationToken.None);
        foreach (var permission in permissions.Order(StringComparer.Ordinal))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        return identity;
    }
}
