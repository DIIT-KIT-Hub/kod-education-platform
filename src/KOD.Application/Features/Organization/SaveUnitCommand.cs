using KOD.Domain.Organization;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Creates or updates an organization unit.
/// </summary>
/// <param name="Code">The unique code.</param>
/// <param name="Name">The name.</param>
/// <param name="ShortName">The abbreviation.</param>
/// <param name="Type">The type.</param>
/// <param name="ParentId">The parent unit.</param>
/// <param name="SortOrder">The display order among siblings.</param>
/// <param name="IsActive">Whether the unit is active.</param>
public sealed record SaveUnitCommand(
    string Code,
    string Name,
    string? ShortName,
    OrganizationUnitType Type,
    Guid? ParentId,
    int SortOrder,
    bool IsActive);
