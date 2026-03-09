using KOD.Domain.Exceptions.Auth;

namespace KOD.Domain.ValueObjects.Otp;

public sealed record OtpCodeDetails
{
    #region Public fields

    /// <summary>
    /// Gets the OTP code value.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Gets the UTC date and time when the OTP code expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpCodeDetails"/> record.
    /// </summary>
    /// <param name="code">The OTP code string.</param>
    /// <param name="expiresAt">The UTC expiration date and time of the OTP code.</param>
    /// <exception cref="CredentialsException">
    /// Thrown if <paramref name="expiresAt"/> is in the past, indicating that the OTP code is already expired.
    /// </exception>
    public OtpCodeDetails(string code, DateTime expiresAt)
    {
        if(expiresAt <= DateTime.UtcNow)
        {
            throw new CredentialsException("Otp code has expired.");
        }

        Code = code;
        ExpiresAt = expiresAt;
    }

    #endregion
}
