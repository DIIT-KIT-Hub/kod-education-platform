using KOD.Domain.Exceptions.Auth;
using KOD.Domain.ValueObjects.Users;

namespace KOD.Domain.ValueObjects.Auth;

/// <summary>
/// Represents the details of a refresh token, including its value, expiration time, and associated user identity.
/// </summary>
public sealed record RefreshTokenDetails
{
    #region Public fields

    /// <summary>
    /// Gets the string value of the refresh token.
    /// </summary>
    public string Token { get; init; }

    /// <summary>
    /// Gets the UTC date and time when the refresh token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Gets the identity of the user associated with this refresh token.
    /// </summary>
    public UserIdentity User { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenDetails"/> record.
    /// </summary>
    /// <param name="token">The string value of the refresh token.</param>
    /// <param name="expiresAt">The UTC expiration date and time of the token.</param>
    /// <param name="user">The <see cref="UserIdentity"/> of the associated user.</param>
    /// <exception cref="CredentialsException">
    /// Thrown if <paramref name="expiresAt"/> is in the past, indicating the token is already expired.
    /// </exception>
    public RefreshTokenDetails(string token, DateTime expiresAt, UserIdentity user)
    {
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new CredentialsException("Refresh token has expired.");
        }

        Token = token;
        ExpiresAt = expiresAt;
        User = user;
    }

    #endregion
}

