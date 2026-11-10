using ODC.Web.Shared.Access;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Reads the permissions of an account in each of its organizations (their databases, one after another).
/// </summary>
/// <param name="scopes">Creates the scope of the master database.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="organizations">Runs work in an organization.</param>
public sealed class AccountPermissions(IServiceScopeFactory scopes, OrganizationRegistry registry, OrganizationScopes organizations) : IAccountPermissions
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> UnionAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<Guid> mine;
        await using (var scope = scopes.CreateAsyncScope())
        {
            mine = await scope.ServiceProvider.GetRequiredService<Memberships>().OfAsync(userId, cancellationToken);
        }

        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in mine)
        {
            if (await registry.FindAsync(id, cancellationToken) is not { IsActive: true } organization)
            {
                continue;
            }

            union.UnionWith(await organizations.RunAsync(
                organization,
                services => services.GetRequiredService<IPermissionResolver>().GetEffectivePermissionsAsync(userId, cancellationToken)));
        }

        return union;
    }
}
