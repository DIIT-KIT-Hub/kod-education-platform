using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Runs the server's own work in the database of an organization: a scope of its own with the organization chosen, so
/// every service (the organization's database, catalogs, positions) works as in a request of that organization.
/// </summary>
/// <param name="scopes">Creates the scopes.</param>
public sealed class OrganizationScopes(IServiceScopeFactory scopes)
{
    /// <summary>
    /// Runs work in an organization.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="organization">The organization.</param>
    /// <param name="work">The work, given the scope's services.</param>
    /// <returns>Its result.</returns>
    public async Task<T> RunAsync<T>(Organization organization, Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<OrganizationContext>().Enter(organization, OrganizationAccess.System);
        return await work(scope.ServiceProvider);
    }

    /// <summary>
    /// Runs work in an organization.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="work">The work, given the scope's services.</param>
    /// <returns>A task.</returns>
    public Task RunAsync(Organization organization, Func<IServiceProvider, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        return RunAsync<bool>(organization, async services =>
        {
            await work(services);
            return true;
        });
    }
}
