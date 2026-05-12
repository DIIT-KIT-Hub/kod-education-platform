namespace KOD.Domain.Entities.Users;

/// <summary>
/// Represents a lightweight identity of a user, containing only the essential identification data.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Email">The email address associated with the user.</param>
public sealed record UserIdentity(Guid Id, string Email);
