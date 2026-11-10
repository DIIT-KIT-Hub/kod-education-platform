using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Access;

namespace ODC.UnitTests.Application;

/// <summary>
/// The permissions of an account in the one organizations' database of the tests.
/// </summary>
/// <param name="resolver">Resolves the permissions in that database.</param>
internal sealed class ResolvedPermissions(IPermissionResolver resolver) : IAccountPermissions
{
    /// <inheritdoc />
    public Task<IReadOnlySet<string>> UnionAsync(Guid userId, CancellationToken cancellationToken) => resolver.GetEffectivePermissionsAsync(userId, cancellationToken);
}
