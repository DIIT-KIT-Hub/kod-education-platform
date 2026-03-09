namespace KOD.Application.DTOs.Auth;

/// <summary>
/// Data transfer object representing a user login request.
/// </summary>
/// <param name="Email">The email of the user.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequestDto(string Email, string Password);

