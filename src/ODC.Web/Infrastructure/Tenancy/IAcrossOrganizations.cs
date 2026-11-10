using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Gathers a person's own work from every organization they work in or take part in (see
/// <see cref="AcrossOrganizations"/>).
/// </summary>
public interface IAcrossOrganizations
{
    /// <summary>
    /// The organizations of a person's own work, the request's first.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organizations.</returns>
    Task<IReadOnlyList<Organization>> OfAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a read in every organization of a person's own work.
    /// </summary>
    /// <typeparam name="T">The result of one organization.</typeparam>
    /// <param name="userId">The person.</param>
    /// <param name="work">The read, given the organization's database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results with their organizations, the request's first.</returns>
    Task<List<Gathered<T>>> GatherAsync<T>(Guid userId, Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken);

    /// <summary>
    /// Runs work with the services of every organization of a person's own work (each in a scope with that organization
    /// entered: handlers and helpers of the organization's side work as in a request there).
    /// </summary>
    /// <typeparam name="T">The result of one organization.</typeparam>
    /// <param name="userId">The person.</param>
    /// <param name="work">The work, given the services of the organization's scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results with their organizations, the request's first.</returns>
    Task<List<Gathered<T>>> GatherInAsync<T>(Guid userId, Func<IServiceProvider, Task<T>> work, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a change in every organization of a person's own work.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="work">The change, given the organization's database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task EachAsync(Guid userId, Func<OrganizationDbContext, Task> work, CancellationToken cancellationToken);

    /// <summary>
    /// The organization a person belongs to first: the default one (earliest membership), else the first they take
    /// part in; null for a person with neither.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or null.</returns>
    Task<Organization?> HomeOfAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Runs work in another organization (a scope of its own with that organization entered).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="work">The work, given the services of the scope.</param>
    /// <returns>A task.</returns>
    Task RunInAsync(Organization organization, Func<IServiceProvider, Task> work);

    /// <summary>
    /// The active people who hold a permission in any organization (e.g. the handlers of requests to the platform
    /// support: <c>tickets.manage</c>), read again a minute after the last time.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    Task<List<Guid>> HoldersAsync(string permission, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a read in every active organization (the platform's figures: how many people hold a role, how many units
    /// have a type).
    /// </summary>
    /// <typeparam name="T">The result of one organization.</typeparam>
    /// <param name="work">The read, given the organization's database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results with their organizations.</returns>
    Task<List<Gathered<T>>> EveryAsync<T>(Func<OrganizationDbContext, Task<T>> work, CancellationToken cancellationToken);
}
