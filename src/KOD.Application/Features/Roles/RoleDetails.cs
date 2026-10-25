namespace KOD.Application.Features.Roles;

/// <summary>
/// Full information about a role.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="IsSystem">Whether the role is predefined.</param>
/// <param name="IsAdministrator">Whether the role is the super-user role.</param>
/// <param name="IsLeadership">Whether the role is a leadership position.</param>
/// <param name="Permissions">The granted permission codes.</param>
/// <param name="UserCount">The number of users holding the role.</param>
public sealed record RoleDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsAdministrator,
    bool IsLeadership,
    IReadOnlyList<string> Permissions,
    int UserCount);
