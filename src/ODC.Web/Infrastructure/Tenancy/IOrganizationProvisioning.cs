using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Creates organizations (each with a database of its own) and keeps the registry in line with their root units.
/// </summary>
public interface IOrganizationProvisioning
{
    /// <summary>
    /// Creates an organization with its database and root unit.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="name">The official name.</param>
    /// <param name="shortName">The abbreviation.</param>
    /// <param name="typeId">The unit type of the root (an organization type).</param>
    /// <param name="sortOrder">The display order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization's identifier (also its root unit's), or an error.</returns>
    Task<Result<Guid>> CreateAsync(string code, string name, string? shortName, Guid typeId, int sortOrder, CancellationToken cancellationToken);

    /// <summary>
    /// Takes the new code and name of an organization's root unit into the registry.
    /// </summary>
    /// <param name="id">The organization (its root unit).</param>
    /// <param name="code">The code.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task RenameAsync(Guid id, string code, string name, CancellationToken cancellationToken);
}
