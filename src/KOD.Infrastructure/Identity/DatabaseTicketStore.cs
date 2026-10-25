using System.Security.Claims;
using KOD.Domain.Sessions;
using KOD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Keeps authentication tickets in PostgreSQL. The browser cookie carries only the (protected) session identifier,
/// which makes sessions listable and instantly revocable. Tickets are encrypted at rest with Data Protection.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the scoped database context.</param>
/// <param name="dataProtection">The data protection provider.</param>
/// <param name="httpContextAccessor">The HTTP context accessor (client IP and user agent).</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class DatabaseTicketStore(
    IServiceScopeFactory scopeFactory,
    IDataProtectionProvider dataProtection,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : ITicketStore
{
    /// <summary>
    /// Protects ticket payloads.
    /// </summary>
    private readonly IDataProtector _protector = dataProtection.CreateProtector("KOD.Sessions.Ticket.v1");

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var sessionId = Guid.CreateVersion7();
        if (ticket.Principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(AppClaimTypes.SessionId, sessionId.ToString()));
        }

        var userId = Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
        var http = httpContextAccessor.HttpContext;
        var session = new UserSession(
            sessionId,
            userId,
            Protect(ticket),
            timeProvider.GetUtcNow(),
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
            .Select(static s => new { s.Ticket, s.ExpiresAt })
            .FirstOrDefaultAsync();

        if (payload is null || payload.ExpiresAt < timeProvider.GetUtcNow())
        {
            return null;
        }

        try
        {
            return TicketSerializer.Default.Deserialize(_protector.Unprotect(payload.Ticket));
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

    /// <summary>
    /// Serializes and encrypts a ticket.
    /// </summary>
    /// <param name="ticket">The ticket.</param>
    /// <returns>The protected payload.</returns>
    private byte[] Protect(AuthenticationTicket ticket) => _protector.Protect(TicketSerializer.Default.Serialize(ticket));
}
