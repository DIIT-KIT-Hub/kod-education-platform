using KOD.Domain.Exceptions.Users;

namespace KOD.Domain.ValueObjects.Users;

/// <summary>
/// Represents login-related information of a user required for authentication.
/// Contains the user identifier, password hash, verification status, and assigned roles.
/// </summary>
public sealed record UserLoginDetails
{
    #region Public fields

    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the hashed password of the user used for credential verification.
    /// </summary>
    public string PasswordHash { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user account has been verified.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// Gets the collection of roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UserLoginDetails"/> record.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="passwordHash">The hashed password used for authentication.</param>
    /// <param name="isVerified">Indicates whether the user account has been verified.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <exception cref="UserNotVerifiedException">
    /// Thrown when the user account is not verified.
    /// </exception>
    public UserLoginDetails(Guid id, string passwordHash, bool isVerified, IEnumerable<string> roles)
    {
        if (!isVerified)
        {
            throw new UserNotVerifiedException();
        }

        Id = id;
        PasswordHash = passwordHash;
        IsVerified = isVerified;
        Roles = roles;
    }

    #endregion
}
