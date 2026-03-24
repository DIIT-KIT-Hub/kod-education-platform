namespace KOD.Domain.ValueObjects.Users;

/// <summary>
/// Represents login-related information of a user required for authentication.
/// Contains the user identifier, password hash, verification status, and assigned roles.
/// </summary>
public sealed record UserLoginDetails(Guid Id, string PasswordHash, bool EmailConfirmed, IEnumerable<string> Roles);
