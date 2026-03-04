namespace KOD.Application.DTOs.Otp;

/// <summary>
/// Data transfer object representing a user for OTP purposes.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Email">The email of the user.</param>
public sealed record UserOtpDto(Guid Id, string Email);
