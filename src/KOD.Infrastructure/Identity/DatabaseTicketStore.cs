using System.Security.Claims;
using KOD.Domain.Sessions;
using KOD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Keeps authentication tickets in PostgreSQL. The browser cookie carries only the (protected) session identifier,
/// which makes sessions listable and instantly revocable. Tickets are encrypted at rest with Data Protection.
/// A session ends after <see cref="SessionLifetimeOptions.IdleTimeout"/> without requests or, at the latest,
/// <see cref="SessionLifetimeOptions.AbsoluteLifetime"/> after sign-in.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the scoped database context.</param>
/// <param name="dataProtection">The data protection provider.</param>
/// <param name="httpContextAccessor">The HTTP context accessor (client IP and user agent).</param>
/// <param name="options">The session lifetime options.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class DatabaseTicketStore(
    IServiceScopeFactory scopeFactory,
    IDataProtectionProvider dataProtection,
    IHttpContextAccessor httpContextAccessor,
    IOptions<SessionLifetimeOptions> options,
    TimeProvider timeProvider) : ITicketStore, IUserSessionValidator
{
    /// <summary>
    /// Protects ticket payloads.
    /// </summary>
    private readonly IDataProtector _protector = dataProtection.CreateProtector("KOD.Sessions.Ticket.v1");

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var now = timeProvider.GetUtcNow();
        var sessionId = Guid.CreateVersion7();
        SetSessionClaim(ticket.Principal, sessionId);
        CapExpiration(ticket.Properties, now);

        var userId = Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
        var http = httpContextAccessor.HttpContext;
        var session = new UserSession(
            sessionId,
            userId,
            Protect(ticket),
            now,
            ticket.Properties.ExpiresUtc,
            http?.Connection.RemoteIpAddress?.ToString(),
            http?.Request.Headers.UserAgent.ToString());

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // A re-issued cookie (sign-in refresh, password change) replaces the session of the current request.
        if (Guid.TryParse(http?.User.FindFirstValue(AppClaimTypes.SessionId), out var previous))
        {
            await db.UserSessions.Where(s => s.Id == previous && s.UserId == userId).ExecuteDeleteAsync();
        }

        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        return sessionId.ToString();
    }

    /// <inheritdoc />
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (!Guid.TryParse(key, out var sessionId))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return;
        }

        // The refreshed principal (security stamp validation) is rebuilt from the database without the session claim.
        SetSessionClaim(ticket.Principal, sessionId);
        CapExpiration(ticket.Properties, session.CreatedAt);
        session.Renew(Protect(ticket), timeProvider.GetUtcNow(), ticket.Properties.ExpiresUtc);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!Guid.TryParse(key, out var sessionId))
        {
            return null;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payload = await db.UserSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(static s => new { s.Ticket, s.CreatedAt, s.ExpiresAt })
            .FirstOrDefaultAsync();

        if (payload is null || !IsAlive(payload.CreatedAt, payload.ExpiresAt))
        {
            return null;
        }

        try
        {
            var ticket = TicketSerializer.Default.Deserialize(_protector.Unprotect(payload.Ticket));
            if (ticket is not null)
            {
                SetSessionClaim(ticket.Principal, sessionId);
            }

            return ticket;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Keys were rotated or the payload was tampered with: treat as signed out.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        if (!Guid.TryParse(key, out var sessionId))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.UserSessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsAliveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.UserSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .Select(static s => new { s.CreatedAt, s.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        return session is not null && IsAlive(session.CreatedAt, session.ExpiresAt);
    }

    /// <summary>
    /// Puts the session identifier into the principal (replacing a stale value).
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="sessionId">The session identifier.</param>
    private static void SetSessionClaim(ClaimsPrincipal principal, Guid sessionId)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var value = sessionId.ToString();
        if (identity.FindFirst(AppClaimTypes.SessionId) is { } existing)
        {
            if (existing.Value == value)
            {
                return;
            }

            identity.RemoveClaim(existing);
        }

        identity.AddClaim(new Claim(AppClaimTypes.SessionId, value));
    }

    /// <summary>
    /// Whether a session with these timestamps is still valid.
    /// </summary>
    /// <param name="createdAt">The sign-in moment.</param>
    /// <param name="expiresAt">The sliding expiration moment.</param>
    /// <returns>The result.</returns>
    private bool IsAlive(DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        var now = timeProvider.GetUtcNow();
        return (expiresAt is null || expiresAt > now) && createdAt + options.Value.AbsoluteLifetime > now;
    }

    /// <summary>
    /// Limits the expiration of a ticket to the absolute lifetime of its session.
    /// </summary>
    /// <param name="properties">The ticket properties.</param>
    /// <param name="createdAt">The sign-in moment.</param>
    private void CapExpiration(AuthenticationProperties properties, DateTimeOffset createdAt)
    {
        var limit = createdAt + options.Value.AbsoluteLifetime;
        if (properties.ExpiresUtc is null || properties.ExpiresUtc > limit)
        {
            properties.ExpiresUtc = limit;
        }
    }

    /// <summary>
    /// Serializes and encrypts a ticket.
    /// </summary>
    /// <param name="ticket">The ticket.</param>
    /// <returns>The protected payload.</returns>
    private byte[] Protect(AuthenticationTicket ticket) => _protector.Protect(TicketSerializer.Default.Serialize(ticket));
}
