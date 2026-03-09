namespace KOD.Application.DTOs.Users;

/// <summary>
/// Data transfer object representing a user for OTP purposes.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Email">The email of the user.</param>
/// <param name="IsVerified">Indicates whether the user account has already been verified.</param>
public sealed record UserOtpDetailsDto(Guid Id, string Email, bool IsVerified);
