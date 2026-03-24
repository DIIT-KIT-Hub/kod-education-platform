namespace KOD.Domain.ValueObjects.Otp;

public sealed record OtpCodeDetails(string Code, DateTime ExpiresAt);
