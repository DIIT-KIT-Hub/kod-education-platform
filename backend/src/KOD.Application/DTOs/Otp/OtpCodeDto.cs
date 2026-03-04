namespace KOD.Application.DTOs.Otp;

/// <summary>
/// Data transfer object representing an OTP code and its expiration time.
/// </summary>
/// <param name="Code">The OTP code value.</param>
/// <param name="ExpiresAt">The expiration date and time of the OTP code.</param>
public sealed record OtpCodeDto(string Code, DateTime ExpiresAt);
