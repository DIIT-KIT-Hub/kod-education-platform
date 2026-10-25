using KOD.Application.Common.Models;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Full information about an organization unit.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="ShortName">The abbreviation.</param>
/// <param name="Type">The type.</param>
/// <param name="IsActive">Whether the unit is active.</param>
/// <param name="ParentId">The parent identifier.</param>
/// <param name="SortOrder">The display order.</param>
/// <param name="Path">The units from the top-level organization to this one (breadcrumbs).</param>
/// <param name="Organization">The nearest organization the unit belongs to (the unit itself for an organization).</param>
/// <param name="Children">The direct child units.</param>
/// <param name="StaffCount">The number of positions in the unit.</param>
/// <param name="CanManage">Whether the current user may edit the unit and add units under it (it is in their organizations).</param>
public sealed record UnitDetails(
    Guid Id,
    string Code,
    string Name,
    string? ShortName,
    UnitTypeRef Type,
    bool IsActive,
    Guid? ParentId,
    int SortOrder,
    IReadOnlyList<UnitRef> Path,
    UnitRef Organization,
    IReadOnlyList<UnitTreeNode> Children,
    int StaffCount,
    bool CanManage);
