namespace KOD.Application.Features.Organization;

/// <summary>
/// Creates or updates a unit type.
/// </summary>
/// <param name="Code">The unique code.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Icon">The icon name.</param>
/// <param name="IsOrganization">Whether units of this type are organizations.</param>
/// <param name="SortOrder">The display order.</param>
public sealed record SaveUnitTypeCommand(
    string Code,
    string Name,
    string? Description,
    string? Icon,
    bool IsOrganization,
    int SortOrder);
