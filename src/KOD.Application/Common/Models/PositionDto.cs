namespace KOD.Application.Common.Models;

/// <summary>
/// A position held by a user.
/// </summary>
/// <param name="UnitId">The unit identifier.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="RoleId">The role identifier.</param>
/// <param name="RoleName">The role name.</param>
/// <param name="Title">The official position title.</param>
/// <param name="IsPrimary">Whether this is the main position.</param>
public sealed record PositionDto(Guid UnitId, string UnitName, Guid RoleId, string RoleName, string? Title, bool IsPrimary)
{
    /// <summary>
    /// Gets the position title, falling back to the role name.
    /// </summary>
    public string DisplayTitle => Title ?? RoleName;
}
