namespace KOD.Application.Common.Access;

/// <summary>
/// Where an administrative permission of the current user applies: in every organization of the cabinet, or in a set of units
/// (the organizations of the positions that grant the permission, with all their units).
/// </summary>
public sealed class AdministrationScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrationScope"/> class.
    /// </summary>
    /// <param name="units">The units, or <see langword="null"/> for the whole cabinet.</param>
    private AdministrationScope(IReadOnlySet<Guid>? units) => Units = units;

    /// <summary>
    /// Gets the scope that covers every organization of the cabinet.
    /// </summary>
    public static AdministrationScope Cabinet { get; } = new(null);

    /// <summary>
    /// Gets the scope that covers nothing.
    /// </summary>
    public static AdministrationScope None { get; } = new(new HashSet<Guid>());

    /// <summary>
    /// Gets the units of the scope, or <see langword="null"/> when it covers the whole cabinet.
    /// </summary>
    public IReadOnlySet<Guid>? Units { get; }

    /// <summary>
    /// Gets a value indicating whether the scope covers every organization of the cabinet.
    /// </summary>
    public bool IsCabinetWide => Units is null;

    /// <summary>
    /// Creates a scope limited to the given units.
    /// </summary>
    /// <param name="units">The units.</param>
    /// <returns>The scope.</returns>
    public static AdministrationScope Of(IReadOnlySet<Guid> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return new AdministrationScope(units);
    }

    /// <summary>
    /// Determines whether a unit is in the scope.
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <returns><see langword="true"/> when the unit is in the scope.</returns>
    public bool Contains(Guid unitId) => Units is null || Units.Contains(unitId);

    /// <summary>
    /// Determines whether an account with positions in the given units is visible: at least one position is in the scope.
    /// Accounts without positions belong to no organization and are visible only in the cabinet-wide scope.
    /// </summary>
    /// <param name="positionUnits">The units of the account's positions.</param>
    /// <returns><see langword="true"/> when the account is visible.</returns>
    public bool Overlaps(IEnumerable<Guid> positionUnits)
    {
        ArgumentNullException.ThrowIfNull(positionUnits);
        return Units is null || positionUnits.Any(Units.Contains);
    }

    /// <summary>
    /// Determines whether an account with positions in the given units belongs entirely to the scope: it has positions and all
    /// of them are in the scope. Only such accounts may be changed as a whole (personal data, blocking, credentials).
    /// </summary>
    /// <param name="positionUnits">The units of the account's positions.</param>
    /// <returns><see langword="true"/> when the account belongs entirely to the scope.</returns>
    public bool Covers(IReadOnlyCollection<Guid> positionUnits)
    {
        ArgumentNullException.ThrowIfNull(positionUnits);
        return Units is null || (positionUnits.Count > 0 && positionUnits.All(Units.Contains));
    }
}
