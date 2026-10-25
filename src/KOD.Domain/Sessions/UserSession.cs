using KOD.Domain.Common;

namespace KOD.Domain.Sessions;

/// <summary>
/// A server-side authentication session. The browser cookie only carries the session identifier;
/// the encrypted authentication ticket is kept here so that sessions can be listed and revoked.
/// </summary>
public sealed class UserSession : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSession"/> class.
    /// </summary>
    /// <param name="id">The session identifier (stored in the cookie).</param>
    /// <param name="userId">The owner of the session.</param>
    /// <param name="ticket">The encrypted serialized authentication ticket.</param>
    /// <param name="createdAt">The moment the session was created.</param>
    /// <param name="expiresAt">The absolute expiration moment.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    public UserSession(
        Guid id,
        Guid userId,
        byte[] ticket,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        UserId = userId;
        Ticket = ticket;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent is { Length: > 512 } ? userAgent[..512] : userAgent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSession"/> class for EF Core materialization.
    /// </summary>
    private UserSession()
    {
        Ticket = [];
    }

    /// <summary>
    /// Gets the owner of the session.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the encrypted serialized authentication ticket.
    /// </summary>
    public byte[] Ticket { get; private set; }

    /// <summary>
    /// Gets the moment the session was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the moment the session was last renewed.
    /// </summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>
    /// Gets the absolute expiration moment.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Gets the client user agent.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Replaces the ticket after a sliding renewal or a security stamp refresh.
    /// </summary>
    /// <param name="ticket">The new encrypted ticket.</param>
    /// <param name="moment">The current moment.</param>
    /// <param name="expiresAt">The new expiration moment.</param>
    public void Renew(byte[] ticket, DateTimeOffset moment, DateTimeOffset? expiresAt)
    {
        Ticket = ticket;
        LastSeenAt = moment;
        ExpiresAt = expiresAt;
    }
}
