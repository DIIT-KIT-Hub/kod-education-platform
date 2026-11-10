using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.UnitTests.Application;

/// <summary>
/// The work of a person across organizations in the tests: every organization shares one in-memory database, so each
/// read runs once on it, in the organization of the request.
/// </summary>
/// <param name="services">The services of the test.</param>
internal sealed class SingleOrganization(IServiceProvider services) : IAcrossOrganizations
{
    /// <summary>
    /// Gets the organization of the request.
    /// </summary>
    private Organization Here => services.GetRequiredService<OrganizationContext>().Require();

    /// <summary>
    /// Gets the shared database.
    /// </summary>
    private OrganizationDbContext Db => services.GetRequiredService<OrganizationDbContext>();

    /// <inheritdoc />
    public Task<IReadOnlyList<Organization>> OfAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Organization>>([Here]);

    /// <inheritdoc />
    public async Task<List<Gathered<T>>> GatherAsync<T>(Guid userId, Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return [new Gathered<T>(Here, await work(Db))];
    }

    /// <inheritdoc />
    public async Task<List<Gathered<T>>> GatherInAsync<T>(Guid userId, Func<IServiceProvider, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return [new Gathered<T>(Here, await work(services))];
    }

    /// <inheritdoc />
    public Task EachAsync(Guid userId, Func<OrganizationDbContext, Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return work(Db);
    }

    /// <inheritdoc />
    public Task<Organization?> HomeOfAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<Organization?>(Here);

    /// <inheritdoc />
    public Task RunInAsync(Organization organization, Func<IServiceProvider, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        return work(services);
    }

    /// <inheritdoc />
    public Task<List<Guid>> HoldersAsync(string permission, CancellationToken cancellationToken)
        => Db.MemberIdsWithAsync(services.GetRequiredService<Catalog>(), permission, cancellationToken);

    /// <inheritdoc />
    public async Task<List<Gathered<T>>> EveryAsync<T>(Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        return [new Gathered<T>(Here, await work(Db))];
    }
}
