using KOD.Domain.Exceptions.Users;

namespace KOD.Domain.ValueObjects.Users;

/// <summary>
/// Represents user information required for OTP verification. Contains the user identifier, email, and verification status.
/// </summary>
public sealed record UserOtpDetails
{
    #region Public fields

    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the email address associated with the user.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user account has already been verified.
    /// </summary>
    public bool IsVerified { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UserOtpDetails"/> record.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="isVerified">Indicates whether the user account has already been verified.</param>
    /// <exception cref="UserVerifiedException">
    /// Thrown when the user account is already verified and OTP verification is not required.
    /// </exception>
    public UserOtpDetails(Guid id, string email, bool isVerified)
    {
        if (isVerified)
        {
            throw new UserVerifiedException();
        }

        Id = id;
        Email = email;
        IsVerified = isVerified;
    }

    #endregion
}
