using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Gathers a person's own work from every organization they work in or take part in (memberships and participations;
/// for a platform administrator not every organization, only those): notifications, documents waiting for their
/// signature, chats. The organization of the request comes first even when it is neither. Each organization is read in a
/// scope of its own, one after another (a person has a handful).
/// </summary>
/// <param name="workplaces">The organizations of people.</param>
/// <param name="scopes">Runs work in an organization.</param>
/// <param name="context">The organization of the request.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="cache">The access cache.</param>
public sealed class AcrossOrganizations(Workplaces workplaces, OrganizationScopes scopes, OrganizationContext context, OrganizationRegistry registry, AccessCache cache) : IAcrossOrganizations
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Organization>> OfAsync(Guid userId, CancellationToken cancellationToken)
    {
        var places = (await workplaces.OfAsync(userId, platform: false, cancellationToken)).Select(static p => p.Organization).ToList();
        if (context.Current is { } here)
        {
            places.RemoveAll(o => o.Id == here.Id);
            places.Insert(0, here);
        }

        return places;
    }

    /// <inheritdoc />
    public Task<List<Gathered<T>>> GatherAsync<T>(Guid userId, Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return GatherInAsync(userId, services => work(services.GetRequiredService<OrganizationDbContext>()), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Gathered<T>>> GatherInAsync<T>(Guid userId, Func<IServiceProvider, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        var result = new List<Gathered<T>>();
        foreach (var organization in await OfAsync(userId, cancellationToken))
        {
            result.Add(new Gathered<T>(organization, await scopes.RunAsync(organization, work)));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task EachAsync(Guid userId, Func<OrganizationDbContext, Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        foreach (var organization in await OfAsync(userId, cancellationToken))
        {
            await scopes.RunAsync(organization, services => work(services.GetRequiredService<OrganizationDbContext>()));
        }
    }

    /// <inheritdoc />
    public async Task<Organization?> HomeOfAsync(Guid userId, CancellationToken cancellationToken)
        => await workplaces.OfAsync(userId, platform: false, cancellationToken) is { Count: > 0 } places ? places[0].Organization : null;

    /// <inheritdoc />
    public Task RunInAsync(Organization organization, Func<IServiceProvider, Task> work) => scopes.RunAsync(organization, work);

    /// <inheritdoc />
    public Task<List<Guid>> HoldersAsync(string permission, CancellationToken cancellationToken)
        => cache.GetAsync($"holders:{permission}", async () =>
        {
            var holders = new HashSet<Guid>();
            foreach (var organization in (await registry.AllAsync(cancellationToken)).Where(static o => o.IsActive))
            {
                holders.UnionWith(await scopes.RunAsync(
                    organization,
                    services => services.GetRequiredService<OrganizationDbContext>().MemberIdsWithAsync(services.GetRequiredService<Catalog>(), permission, cancellationToken)));
            }

            return holders.ToList();
        });

    /// <inheritdoc />
    public async Task<List<Gathered<T>>> EveryAsync<T>(Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        var result = new List<Gathered<T>>();
        foreach (var organization in (await registry.AllAsync(cancellationToken)).Where(static o => o.IsActive))
        {
            result.Add(new Gathered<T>(organization, await scopes.RunAsync(organization, services => work(services.GetRequiredService<OrganizationDbContext>()))));
        }

        return result;
    }
}
