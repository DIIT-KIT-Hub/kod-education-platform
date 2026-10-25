namespace KOD.Application.Features.Users;

/// <summary>
/// One-time credentials of a new or reset account. The temporary password must be changed at the first sign-in.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Email">The login.</param>
/// <param name="FullName">The full name.</param>
/// <param name="TemporaryPassword">The temporary password.</param>
public sealed record UserCredentials(Guid UserId, string Email, string FullName, string TemporaryPassword);
