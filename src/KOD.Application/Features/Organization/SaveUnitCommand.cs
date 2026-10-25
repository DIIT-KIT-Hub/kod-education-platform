namespace KOD.Application.Features.Organization;

/// <summary>
/// Creates or updates an organization or a unit.
/// </summary>
/// <param name="Code">The unique code.</param>
/// <param name="Name">The name.</param>
/// <param name="ShortName">The abbreviation.</param>
/// <param name="TypeId">The unit type identifier.</param>
/// <param name="ParentId">The parent unit, <see langword="null"/> for a top-level organization.</param>
/// <param name="SortOrder">The display order among siblings.</param>
/// <param name="IsActive">Whether the unit is active.</param>
public sealed record SaveUnitCommand(
    string Code,
    string Name,
    string? ShortName,
    Guid TypeId,
    Guid? ParentId,
    int SortOrder,
    bool IsActive);
