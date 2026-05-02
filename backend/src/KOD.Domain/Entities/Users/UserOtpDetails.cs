namespace KOD.Domain.Entities.Users;

/// <summary>
/// Represents user information required for OTP verification. Contains the user identifier, email, and verification status.
/// </summary>
public sealed record UserOtpDetails(Guid Id, string Email, bool EmailConfirmed);
