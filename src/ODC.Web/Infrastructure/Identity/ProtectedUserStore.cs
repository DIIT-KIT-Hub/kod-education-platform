using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Identity;

/// <summary>
/// The Identity user store (users, roles, claims, logins, tokens) with encrypted secret tokens: the authenticator (TOTP)
/// key and the recovery codes are never stored in plain text (see <see cref="UserTokenProtector"/>).
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="protector">The token protector.</param>
/// <param name="describer">The error describer.</param>
internal sealed class ProtectedUserStore(MasterDbContext context, UserTokenProtector protector, IdentityErrorDescriber? describer = null)
    : UserOnlyStore<User, MasterDbContext, Guid, IdentityUserClaim<Guid>, IdentityUserLogin<Guid>, IdentityUserToken<Guid>>(context, describer)
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
