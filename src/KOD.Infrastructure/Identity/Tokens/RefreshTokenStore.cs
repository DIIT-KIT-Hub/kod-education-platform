using System.Security.Cryptography;
using KOD.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// Refresh token rotation with replay detection (RFC 9700 §2.2.2, §4.14.2): every refresh invalidates the presented token
/// and issues the next generation; an invalidated generation coming back revokes the whole family. Families also end on
/// sign-out, after the idle timeout or the absolute lifetime, and when the user's security stamp changes. Each operation
/// uses its own database context, so the context of the request stays untouched.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the database context.</param>
/// <param name="dataProtection">Protects the family keys at rest.</param>
/// <param name="lifetime">The sign-in lifetime.</param>
/// <param name="jwt">The token options (reuse interval).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class RefreshTokenStore(
    IServiceScopeFactory scopeFactory,
    IDataProtectionProvider dataProtection,
    IOptions<SessionLifetimeOptions> lifetime,
    IOptions<JwtOptions> jwt,
    TimeProvider timeProvider,
    ILogger<RefreshTokenStore> logger)
{
    /// <summary>
    /// Protects the family keys.
    /// </summary>
    private readonly IDataProtector _protector = dataProtection.CreateProtector("KOD.Authentication.RefreshTokenFamily.v1");

    /// <summary>
    /// Starts a refresh token family for a completed sign-in.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="securityStamp">The user's current security stamp.</param>
    /// <param name="authenticationMethod">The authentication method (RFC 8176).</param>
    /// <param name="isPersistent">Whether the sign-in survives closing the browser.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in and its first refresh token.</returns>
    public async Task<RefreshResult> CreateAsync(
        Guid userId,
        string securityStamp,
        string? authenticationMethod,
        bool isPersistent,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var now = Truncate(timeProvider.GetUtcNow());
        var key = RefreshToken.NewKey();
        try
        {
            var family = new RefreshTokenFamily(userId, _protector.Protect(key), securityStamp, authenticationMethod, isPersistent, now, ExpiresAt(now, now), ipAddress, userAgent);
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RefreshTokenFamilies.Add(family);
            await db.SaveChangesAsync(cancellationToken);
            return Refreshed(family, RefreshToken.Format(key, family.Id, family.Generation));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Uses a refresh token. The current generation is rotated; the previous one, within the reuse interval after the
    /// rotation, gets the current token without another rotation (requests sent in parallel); any other authentic but
    /// invalidated generation revokes the family.
    /// </summary>
    /// <param name="token">The refresh token from the cookie.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome.</returns>
    public async Task<RefreshResult> RefreshAsync(string token, string? ipAddress, CancellationToken cancellationToken)
    {
        if (!RefreshToken.TryParse(token, out var familyId, out var generation))
        {
            return new RefreshResult(RefreshStatus.Invalid);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await db.RefreshTokenFamilies.FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken);
        if (family is null || TryUnprotect(family.ProtectedKey) is not { } key)
        {
            return new RefreshResult(RefreshStatus.Invalid);
        }

        try
        {
            // Without a valid MAC the token was not issued by this server: reject it without touching the family.
            if (!RefreshToken.Verify(token, key))
            {
                return new RefreshResult(RefreshStatus.Invalid);
            }

            var now = timeProvider.GetUtcNow();
            if (family.ExpiresAt <= now)
            {
                await DeleteAsync(db, family, cancellationToken);
                return new RefreshResult(RefreshStatus.Invalid, family.UserId);
            }

            if (generation == family.Generation)
            {
                family.Rotate(now, ExpiresAt(family.CreatedAt, now), ipAddress);
                try
                {
                    // The generation is a concurrency token: of two parallel refreshes only one rotates.
                    await db.SaveChangesAsync(cancellationToken);
                    return Refreshed(family, RefreshToken.Format(key, family.Id, family.Generation));
                }
                catch (DbUpdateConcurrencyException)
                {
                    db.ChangeTracker.Clear();
                    family = await db.RefreshTokenFamilies.AsNoTracking().FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken);
                    if (family is null)
                    {
                        return new RefreshResult(RefreshStatus.Invalid);
                    }
                }
            }

            if (generation == family.Generation - 1 && family.RotatedAt + jwt.Value.RefreshTokenReuseInterval >= now)
            {
                return Refreshed(family, RefreshToken.Format(key, family.Id, family.Generation));
            }

            if (generation < family.Generation)
            {
                LogReuse(logger, family.Id, family.UserId, generation, family.Generation);
                db.ChangeTracker.Clear();
                await DeleteAsync(db, family, cancellationToken);
                return new RefreshResult(RefreshStatus.Reused, family.UserId);
            }

            return new RefreshResult(RefreshStatus.Invalid, family.UserId);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Binds a live family to the user's new security stamp (the password was changed in this browser), so this sign-in
    /// continues while the user's other families can no longer be refreshed.
    /// </summary>
    /// <param name="familyId">The family of the current request.</param>
    /// <param name="userId">The user.</param>
    /// <param name="securityStamp">The new security stamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in and the expiry of its refresh token, or <see langword="null"/> when the family has ended.</returns>
    public async Task<RefreshResult?> RebindAsync(Guid familyId, Guid userId, string securityStamp, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var family = await db.RefreshTokenFamilies.FirstOrDefaultAsync(f => f.Id == familyId && f.UserId == userId && f.ExpiresAt > now, cancellationToken);
        if (family is null)
        {
            return null;
        }

        if (family.SecurityStamp != securityStamp)
        {
            family.Rebind(securityStamp);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Refreshed(family, token: null);
    }

    /// <summary>
    /// Determines whether a sign-in is still alive (used by interactive circuits, which outlive their access token).
    /// </summary>
    /// <param name="familyId">The family.</param>
    /// <param name="userId">The user.</param>
    /// <param name="securityStamp">The user's current security stamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the family exists, has not expired and is bound to the current stamp.</returns>
    public async Task<bool> IsAliveAsync(Guid familyId, Guid userId, string? securityStamp, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        return await db.RefreshTokenFamilies.AnyAsync(
            f => f.Id == familyId && f.UserId == userId && f.ExpiresAt > now && f.SecurityStamp == securityStamp,
            cancellationToken);
    }

    /// <summary>
    /// Ends a sign-in (sign-out, or a new sign-in replacing it in the same browser).
    /// </summary>
    /// <param name="familyId">The family.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the family is gone.</returns>
    public async Task RevokeAsync(Guid familyId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await db.RefreshTokenFamilies.FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken) is { } family)
        {
            await DeleteAsync(db, family, cancellationToken);
        }
    }

    /// <summary>
    /// Ends the sign-in of a refresh token, when the token is authentic (sign-out without a valid access token).
    /// </summary>
    /// <param name="token">The refresh token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the family is gone.</returns>
    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        if (!RefreshToken.TryParse(token, out var familyId, out _))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await db.RefreshTokenFamilies.FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken);
        if (family is not null && TryUnprotect(family.ProtectedKey) is { } key)
        {
            var authentic = RefreshToken.Verify(token, key);
            CryptographicOperations.ZeroMemory(key);
            if (authentic)
            {
                await DeleteAsync(db, family, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Builds a successful result.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <param name="token">The refresh token to keep, or <see langword="null"/> to keep the current cookie.</param>
    /// <returns>The result.</returns>
    private static RefreshResult Refreshed(RefreshTokenFamily family, string? token)
        => new(
            RefreshStatus.Refreshed,
            family.UserId,
            new SignInContext(family.Id, family.CreatedAt, family.AuthenticationMethod, family.IsPersistent),
            family.SecurityStamp,
            token,
            family.ExpiresAt);

    /// <summary>
    /// Truncates a moment to whole seconds, the resolution of JWT times.
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The truncated moment.</returns>
    private static DateTimeOffset Truncate(DateTimeOffset moment) => DateTimeOffset.FromUnixTimeSeconds(moment.ToUnixTimeSeconds());

    /// <summary>
    /// Deletes a family; a parallel request may already have done it.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="family">The family.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the family is deleted.</returns>
    private static async Task DeleteAsync(ApplicationDbContext db, RefreshTokenFamily family, CancellationToken cancellationToken)
    {
        db.RefreshTokenFamilies.Remove(family);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Rotated or deleted by a parallel request: it is ended either way at the next attempt.
        }
    }

    /// <summary>
    /// Logs the reuse of an invalidated refresh token.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="familyId">The family.</param>
    /// <param name="userId">The user.</param>
    /// <param name="presented">The presented generation.</param>
    /// <param name="current">The current generation.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token reuse: generation {Presented} of family {FamilyId} (user {UserId}) came back after generation {Current} was issued; the family was revoked")]
    private static partial void LogReuse(ILogger logger, Guid familyId, Guid userId, int presented, int current);

    /// <summary>
    /// Computes the idle expiration: the idle timeout from now, but never past the absolute lifetime.
    /// </summary>
    /// <param name="createdAt">The sign-in moment.</param>
    /// <param name="now">The current moment.</param>
    /// <returns>The expiration.</returns>
    private DateTimeOffset ExpiresAt(DateTimeOffset createdAt, DateTimeOffset now)
    {
        var idle = now + lifetime.Value.IdleTimeout;
        var absolute = createdAt + lifetime.Value.AbsoluteLifetime;
        return idle < absolute ? idle : absolute;
    }

    /// <summary>
    /// Decrypts a family key.
    /// </summary>
    /// <param name="protectedKey">The encrypted key.</param>
    /// <returns>The key, or <see langword="null"/> when the Data Protection keys changed.</returns>
    private byte[]? TryUnprotect(byte[] protectedKey)
    {
        try
        {
            return _protector.Unprotect(protectedKey);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
