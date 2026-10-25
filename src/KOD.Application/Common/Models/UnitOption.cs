namespace KOD.Application.Common.Models;

/// <summary>
/// An organization unit option for hierarchical select lists.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Name">The unit name.</param>
/// <param name="Code">The unit code.</param>
/// <param name="IsOrganization">Whether the unit is an organization.</param>
/// <param name="Depth">The depth in the tree (0 for a top-level organization).</param>
public sealed record UnitOption(Guid Id, string Name, string Code, bool IsOrganization, int Depth);
