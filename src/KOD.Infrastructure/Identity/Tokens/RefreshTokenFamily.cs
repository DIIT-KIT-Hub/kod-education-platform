using KOD.Domain.Common;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// The server-side state of one sign-in: the family of refresh tokens issued for it (RFC 9700 §4.14.2). Every refresh
/// rotates the token to the next <see cref="Generation"/>; presenting any older generation means a copy is in someone
/// else's hands, and the whole family is revoked. Deleting the row signs that browser out.
/// </summary>
/// <remarks>
/// The tokens themselves are not stored: a token of generation <c>n</c> is <c>HMAC-SHA256(key, id ‖ n)</c> with a random
/// per-family key that is kept encrypted with Data Protection, so every generation can be verified (and the current one
/// handed out again to a request that raced a rotation) while a database dump alone is useless.
/// </remarks>
public sealed class RefreshTokenFamily : Entity
{
    /// <summary>
    /// Maximum length of <see cref="UserAgent"/>.
    /// </summary>
    public const int UserAgentMaxLength = 512;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenFamily"/> class.
    /// </summary>
    /// <param name="userId">The user who signed in.</param>
    /// <param name="protectedKey">The family's MAC key, encrypted with Data Protection.</param>
    /// <param name="securityStamp">The security stamp of the user at sign-in.</param>
    /// <param name="authenticationMethod">The authentication method (RFC 8176 <c>amr</c>).</param>
    /// <param name="isPersistent">Whether the sign-in survives closing the browser.</param>
    /// <param name="createdAt">The sign-in moment (<c>auth_time</c>).</param>
    /// <param name="expiresAt">The end of the idle timeout (never after the absolute lifetime).</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    public RefreshTokenFamily(
        Guid userId,
        byte[] protectedKey,
        string securityStamp,
        string? authenticationMethod,
        bool isPersistent,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        UserId = userId;
        ProtectedKey = protectedKey;
        SecurityStamp = securityStamp;
        AuthenticationMethod = authenticationMethod;
        IsPersistent = isPersistent;
        CreatedAt = createdAt;
        LastUsedAt = createdAt;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent is { Length: > UserAgentMaxLength } ? userAgent[..UserAgentMaxLength] : userAgent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenFamily"/> class for EF Core materialization.
    /// </summary>
    private RefreshTokenFamily()
    {
        ProtectedKey = [];
        SecurityStamp = string.Empty;
    }

    /// <summary>
    /// Gets the user who signed in.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the family's MAC key, encrypted with Data Protection.
    /// </summary>
    public byte[] ProtectedKey { get; private set; }

    /// <summary>
    /// Gets the generation of the only valid refresh token (0 for the token issued at sign-in).
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    /// Gets the moment of the last rotation.
    /// </summary>
    public DateTimeOffset? RotatedAt { get; private set; }

    /// <summary>
    /// Gets the security stamp of the user the family was issued for; after a new stamp (password change, block,
    /// "sign out everywhere") the family can no longer be refreshed.
    /// </summary>
    public string SecurityStamp { get; private set; }

    /// <summary>
    /// Gets the authentication method of the sign-in (RFC 8176).
    /// </summary>
    public string? AuthenticationMethod { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the sign-in survives closing the browser.
    /// </summary>
    public bool IsPersistent { get; private set; }

    /// <summary>
    /// Gets the sign-in moment; the absolute lifetime counts from it.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the moment of the last refresh.
    /// </summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>
    /// Gets the moment the family expires without use; it slides with every rotation up to the absolute lifetime.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the client IP address of the last refresh.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Gets the client user agent at sign-in.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Moves the family to the next generation: the presented token becomes invalid.
    /// </summary>
    /// <param name="moment">The current moment.</param>
    /// <param name="expiresAt">The new idle expiration.</param>
    /// <param name="ipAddress">The client IP address.</param>
    public void Rotate(DateTimeOffset moment, DateTimeOffset expiresAt, string? ipAddress)
    {
        Generation++;
        RotatedAt = moment;
        LastUsedAt = moment;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress ?? IpAddress;
    }

    /// <summary>
    /// Binds the family to the user's new security stamp: the password was changed (or other devices signed out) in this
    /// browser, which stays signed in while the other families can no longer be refreshed.
    /// </summary>
    /// <param name="securityStamp">The new security stamp.</param>
    public void Rebind(string securityStamp) => SecurityStamp = securityStamp;
}
