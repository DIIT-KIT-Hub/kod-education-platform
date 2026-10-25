namespace KOD.Domain.Access;

/// <summary>
/// Describes a predefined role that is created on the first start of the system.
/// </summary>
/// <param name="Code">The stable role code.</param>
/// <param name="Name">The user-facing role name.</param>
/// <param name="Description">The user-facing role description.</param>
/// <param name="Permissions">The default permissions of the role.</param>
/// <param name="IsLeadership">Whether the role is a leadership position.</param>
public sealed record SystemRoleDefinition(string Code, string Name, string Description, IReadOnlyList<string> Permissions, bool IsLeadership);
