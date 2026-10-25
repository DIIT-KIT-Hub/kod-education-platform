using KOD.Domain.Organization;

namespace KOD.Application.Features.Organization;

/// <summary>
/// A node of the organization tree.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Code">The unit code.</param>
/// <param name="Name">The unit name.</param>
/// <param name="ShortName">The abbreviation.</param>
/// <param name="Type">The unit type.</param>
/// <param name="IsActive">Whether the unit is active.</param>
/// <param name="StaffCount">The number of positions in the unit.</param>
/// <param name="Children">The child nodes.</param>
public sealed record UnitTreeNode(
    Guid Id,
    string Code,
    string Name,
    string? ShortName,
    OrganizationUnitType Type,
    bool IsActive,
    int StaffCount,
    IReadOnlyList<UnitTreeNode> Children);
