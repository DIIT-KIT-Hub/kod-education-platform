using KOD.Application.Common.Models;

namespace KOD.Application.Features.Users;

/// <summary>
/// A position of a user as seen by an administrator.
/// </summary>
/// <param name="UnitId">The unit identifier.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="Organization">The organization of the unit.</param>
/// <param name="RoleId">The role identifier.</param>
/// <param name="RoleName">The role name.</param>
/// <param name="Title">The official position title.</param>
/// <param name="IsPrimary">Whether this is the main position.</param>
/// <param name="IsManaged">Whether the unit is in the organizations the current user manages accounts of.</param>
public sealed record UserPositionDto(
    Guid UnitId,
    string UnitName,
    UnitRef Organization,
    Guid RoleId,
    string RoleName,
    string? Title,
    bool IsPrimary,
    bool IsManaged);
