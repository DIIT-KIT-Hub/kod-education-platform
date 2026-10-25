using KOD.Domain.Organization;

namespace KOD.Application.Features.Organization;

/// <summary>
/// A row of the unit management grid.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="Type">The type.</param>
/// <param name="ParentName">The parent unit name.</param>
/// <param name="IsActive">Whether the unit is active.</param>
/// <param name="StaffCount">The number of positions.</param>
public sealed record UnitListItem(Guid Id, string Code, string Name, OrganizationUnitType Type, string? ParentName, bool IsActive, int StaffCount);
