namespace KOD.Application.DTOs.Auth;

/// <summary>
/// Represents authentication and authorization information for a user,
/// including the assigned role and permissions.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Role">The role assigned to the user.</param>
/// <param name="Permissions">The collection of permissions granted to the user.</param>
public sealed record UserAuthInfoDto(Guid Id, string Role, IEnumerable<string> Permissions);
