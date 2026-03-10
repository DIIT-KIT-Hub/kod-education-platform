namespace KOD.Domain.Entities.Otp;

/// <summary>
/// Represents a one-time password (OTP) code associated with a user.
/// </summary>
public sealed class OtpCode
{
    #region Public fields

    /// <summary>
    /// Gets or sets the unique identifier of the OTP code.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the OTP code value.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date and time of the OTP code.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user associated with this OTP code.
    /// </summary>
    public Guid UserId { get; set; }

    #endregion
}
