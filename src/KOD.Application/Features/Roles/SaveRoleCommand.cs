namespace KOD.Application.Features.Roles;

/// <summary>
/// Creates or updates a role.
/// </summary>
/// <param name="Code">The code (ignored when updating).</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Permissions">The granted permission codes.</param>
public sealed record SaveRoleCommand(string Code, string Name, string? Description, IReadOnlyList<string> Permissions);
