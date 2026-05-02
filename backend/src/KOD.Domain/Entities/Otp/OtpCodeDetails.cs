namespace KOD.Domain.Entities.Otp;

public sealed record OtpCodeDetails(string Code, DateTime ExpiresAt);
