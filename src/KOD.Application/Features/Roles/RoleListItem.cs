namespace KOD.Application.Features.Roles;

/// <summary>
/// A row of the role grid.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="IsSystem">Whether the role is predefined.</param>
/// <param name="IsLeadership">Whether the role is a leadership position.</param>
/// <param name="PermissionCount">The number of granted permissions.</param>
/// <param name="UserCount">The number of users holding the role.</param>
public sealed record RoleListItem(Guid Id, string Code, string Name, string? Description, bool IsSystem, bool IsLeadership, int PermissionCount, int UserCount);
