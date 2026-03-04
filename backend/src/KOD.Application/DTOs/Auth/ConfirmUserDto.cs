namespace KOD.Application.DTOs.Auth;

/// <summary>
/// Data transfer object used for confirming a user's credentials and OTP code.
/// </summary>
/// <param name="Username">The username of the user.</param>
/// <param name="Password">The user's password.</param>
/// <param name="OtpCode">The one-time password (OTP) code.</param>
public sealed record ConfirmUserDto(string Username, string Password, string OtpCode);

