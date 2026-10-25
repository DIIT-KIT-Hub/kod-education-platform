using KOD.Domain.Organization;

namespace KOD.Application.Common.Models;

/// <summary>
/// An organization unit option for hierarchical select lists.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Name">The unit name.</param>
/// <param name="Code">The unit code.</param>
/// <param name="Type">The unit type.</param>
/// <param name="Depth">The depth in the tree (0 for the root).</param>
public sealed record UnitOption(Guid Id, string Name, string Code, OrganizationUnitType Type, int Depth);
