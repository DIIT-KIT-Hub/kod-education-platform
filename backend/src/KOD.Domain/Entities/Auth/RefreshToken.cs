using KOD.Domain.Entities.Users;

namespace KOD.Domain.Entities.Auth;

/// <summary>
/// Represents a refresh token used to obtain a new access token.
/// </summary>
public sealed class RefreshToken
{
    #region Public fields

    /// <summary>
    /// Gets or sets the unique identifier of the refresh token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date and time of the refresh token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user associated with this refresh token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user associated with this refresh token.
    /// </summary>
    public ApplicationUser? User { get; set; }

    #endregion
}
