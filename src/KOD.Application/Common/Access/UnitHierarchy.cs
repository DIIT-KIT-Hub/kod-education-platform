using KOD.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Access;

/// <summary>
/// The organization unit hierarchy loaded into memory: parents, organizations and names of every unit.
/// </summary>
internal sealed class UnitHierarchy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitHierarchy"/> class.
    /// </summary>
    /// <param name="units">Every unit.</param>
    private UnitHierarchy(List<UnitNode> units)
    {
        Parents = units.ToDictionary(static u => u.Id, static u => u.ParentId);
        Organizations = units.Where(static u => u.IsOrganization).Select(static u => u.Id).ToHashSet();
        Names = units.ToDictionary(static u => u.Id, static u => u.Name);
    }

    /// <summary>
    /// Gets the parent of every unit.
    /// </summary>
    public IReadOnlyDictionary<Guid, Guid?> Parents { get; }

    /// <summary>
    /// Gets the units whose type is an organization.
    /// </summary>
    public IReadOnlySet<Guid> Organizations { get; }

    /// <summary>
    /// Gets the name of every unit.
    /// </summary>
    public IReadOnlyDictionary<Guid, string> Names { get; }

    /// <summary>
    /// Loads the hierarchy of every unit.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hierarchy.</returns>
    public static async Task<UnitHierarchy> LoadAsync(IApplicationDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var units = await db.OrganizationUnits
            .AsNoTracking()
            .Select(static u => new UnitNode(u.Id, u.ParentId, u.Type!.IsOrganization, u.Name))
            .ToListAsync(cancellationToken);
        return new UnitHierarchy(units);
    }

    /// <summary>
    /// Finds the organization of a unit (see <see cref="OrganizationTree.OrganizationOf"/>).
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <returns>The organization identifier.</returns>
    public Guid OrganizationOf(Guid unitId) => OrganizationTree.OrganizationOf(Parents, Organizations, unitId);

    /// <summary>
    /// Collects the given units and all their descendants.
    /// </summary>
    /// <param name="roots">The starting units.</param>
    /// <returns>The units.</returns>
    public HashSet<Guid> WithDescendants(IEnumerable<Guid> roots)
        => OrganizationTree.WithDescendants(Parents.Select(static p => (p.Key, p.Value)), roots);

    /// <summary>
    /// Collects the organizations of the given units together with all their units.
    /// </summary>
    /// <param name="units">The units (usually of someone's positions).</param>
    /// <returns>The units of those organizations.</returns>
    public HashSet<Guid> OrganizationsOf(IEnumerable<Guid> units)
        => WithDescendants(units.Where(Parents.ContainsKey).Select(OrganizationOf));

    /// <summary>
    /// A unit with its parent.
    /// </summary>
    /// <param name="Id">The unit identifier.</param>
    /// <param name="ParentId">The parent identifier.</param>
    /// <param name="IsOrganization">Whether the unit is an organization.</param>
    /// <param name="Name">The unit name.</param>
    private sealed record UnitNode(Guid Id, Guid? ParentId, bool IsOrganization, string Name);
}
