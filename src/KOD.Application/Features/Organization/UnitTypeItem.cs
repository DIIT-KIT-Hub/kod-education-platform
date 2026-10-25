namespace KOD.Application.Features.Organization;

/// <summary>
/// A unit type with its usage.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Icon">The icon name.</param>
/// <param name="IsOrganization">Whether units of this type are organizations.</param>
/// <param name="SortOrder">The display order.</param>
/// <param name="UnitCount">The number of units of this type.</param>
public sealed record UnitTypeItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Icon,
    bool IsOrganization,
    int SortOrder,
    int UnitCount);
