using KOD.Domain.Access;
using KOD.Domain.Users;
using KOD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// The Identity user store (users, roles, claims, logins, tokens) with encrypted secret tokens: the authenticator (TOTP)
/// key and the recovery codes are never stored in plain text (see <see cref="UserTokenProtector"/>).
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="protector">The token protector.</param>
/// <param name="describer">The error describer.</param>
internal sealed class ProtectedUserStore(ApplicationDbContext context, UserTokenProtector protector, IdentityErrorDescriber? describer = null)
    : UserStore<User, Role, ApplicationDbContext, Guid, UserPermissionOverride, StaffPosition, IdentityUserLogin<Guid>, IdentityUserToken<Guid>, RolePermission>(context, describer)
{
    /// <inheritdoc />
    public override Task SetTokenAsync(User user, string loginProvider, string name, string? value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return base.SetTokenAsync(user, loginProvider, name, protector.Protect(user.Id, loginProvider, name, value), cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<string?> GetTokenAsync(User user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return protector.Unprotect(user.Id, loginProvider, name, await base.GetTokenAsync(user, loginProvider, name, cancellationToken));
    }
}
