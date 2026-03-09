namespace KOD.Application.DTOs.Users;

/// <summary>
/// Data transfer object used for confirming a user's credentials and OTP code.
/// </summary>
/// <param name="Email">The email of the user.</param>
/// <param name="Password">The user's password.</param>
/// <param name="OtpCode">The one-time password (OTP) code.</param>
public sealed record ConfirmUserDto(string Email, string Password, string OtpCode);

