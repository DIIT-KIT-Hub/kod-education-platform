using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// Activates accounts created by administrators: the owner types the e-mail, receives a one-time code, enters it in the
/// same browser and chooses the first password (then enrolls the authenticator and uploads a photo through the usual
/// sign-in steps). Nobody but the owner ever knows the password.
/// </summary>
/// <remarks>
/// <para>
/// Security (OWASP Authentication and Forgot Password cheat sheets, NIST SP 800-63B-4): six digits from a CSPRNG, kept
/// encrypted, valid for <see cref="ActivationOptions.CodeLifetime"/>, single use, at most
/// <see cref="ActivationOptions.MaxFailedAttempts"/> wrong guesses per code, a pause and an hourly limit per address,
/// bound to the browser by an encrypted <c>HttpOnly</c> cookie (a code alone is useless elsewhere).
/// </para>
/// <para>
/// No account enumeration: every valid address gets the same answer, the same limits and the same minimum duration; a
/// request for an address without an account waiting for activation is stored too, but no e-mail is sent and no code
/// can match it.
/// </para>
/// </remarks>
/// <param name="db">The database context.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="outbox">Sends the e-mails.</param>
/// <param name="emailOptions">The e-mail settings (the address of the cabinet).</param>
/// <param name="options">The activation limits.</param>
/// <param name="dataProtection">Encrypts the codes and the cookie.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
public sealed partial class AccountActivationService(
    ApplicationDbContext db,
    UserManager<User> userManager,
    IEmailOutbox outbox,
    IOptions<EmailOptions> emailOptions,
    IOptions<ActivationOptions> options,
    IDataProtectionProvider dataProtection,
    IAuditLogger audit,
    TimeProvider timeProvider,
    ILogger<AccountActivationService> logger)
{
    /// <summary>
    /// The error code of a code that cannot be entered any more (expired, replaced, too many wrong guesses).
    /// </summary>
    public const string ExpiredCode = "activation_expired";

    /// <summary>
    /// The error code of a wrong code.
    /// </summary>
    public const string InvalidCode = "activation_invalid";

    /// <summary>
    /// How long the cookie of an activation is valid.
    /// </summary>
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Encrypts the codes stored in the database.
    /// </summary>
    private readonly IDataProtector _codeProtector = dataProtection.CreateProtector("ODC.Identity.ActivationCode.v1");

    /// <summary>
    /// Encrypts the cookie that binds an activation to a browser.
    /// </summary>
    private readonly ITimeLimitedDataProtector _ticketProtector = dataProtection.CreateProtector("ODC.Identity.ActivationTicket.v1").ToTimeLimitedDataProtector();

    /// <summary>
    /// Gets the limits.
    /// </summary>
    private ActivationOptions Limits => options.Value;

    /// <summary>
    /// Requests a code for an address. Every valid address gets the same answer; only an active account waiting for
    /// activation receives an e-mail.
    /// </summary>
    /// <param name="email">The address typed by the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cookie value and the state, or an error (invalid address, too many requests, no e-mail).</returns>
    public async Task<Result<ActivationStart>> StartAsync(string? email, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        var result = await IssueAsync(email, cancellationToken);
        var remaining = Limits.MinimumRequestDuration - timeProvider.GetElapsedTime(started);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, timeProvider, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Reads the activation of a browser from its cookie.
    /// </summary>
    /// <param name="ticket">The cookie value.</param>
    /// <returns>The activation, or <see langword="null"/> when there is none or it is not valid.</returns>
    public ActivationFlow? ReadTicket(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket))
        {
            return null;
        }

        try
        {
            var payload = _ticketProtector.Unprotect(ticket);
            var separator = payload.IndexOf('|', StringComparison.Ordinal);
            return separator > 0 && Guid.TryParseExact(payload[..separator], "N", out var id)
                ? new ActivationFlow(id, payload[(separator + 1)..])
                : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the state of an activation (after a reload of the page).
    /// </summary>
    /// <param name="flow">The activation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state.</returns>
    public async Task<ActivationStatus> GetStatusAsync(ActivationFlow flow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(flow);
        var now = timeProvider.GetUtcNow();
        var challenge = await db.ActivationChallenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == flow.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return new ActivationStatus(ActivationStage.Expired, flow.Email, 0);
        }

        var stage = !challenge.IsUsable(now, Limits.MaxFailedAttempts) || await IsReplacedAsync(challenge, cancellationToken)
            ? ActivationStage.Expired
            : challenge.VerifiedAt is null ? ActivationStage.Code : ActivationStage.Password;
        return new ActivationStatus(stage, flow.Email, ResendAfter(challenge.CreatedAt, now));
    }

    /// <summary>
    /// Checks a code. A wrong code counts toward the limit of its request; the right one opens the password step.
    /// </summary>
    /// <param name="flow">The activation.</param>
    /// <param name="code">The code typed by the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or an error (<see cref="InvalidCode"/>, <see cref="ExpiredCode"/>).</returns>
    public async Task<Result<ActivatedAccount>> VerifyAsync(ActivationFlow flow, string? code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(flow);
        var digits = new string((code ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        // Parallel guesses of one code collide on the row version: each is counted after a reload, none slips through.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var now = timeProvider.GetUtcNow();
            var challenge = await db.ActivationChallenges.FirstOrDefaultAsync(c => c.Id == flow.ChallengeId, cancellationToken);
            if (challenge is null || !challenge.IsUsable(now, Limits.MaxFailedAttempts) || await IsReplacedAsync(challenge, cancellationToken))
            {
                return Expired();
            }

            var user = challenge.UserId is { } userId ? await userManager.FindByIdAsync(userId.ToString()) : null;
            if (challenge.VerifiedAt is not null)
            {
                // Entered before (the page was reloaded): the password step is still open.
                return user is { IsActive: true, AwaitsActivation: true } ? Activated(user) : Expired();
            }

            if (user is not null && Matches(challenge, digits))
            {
                if (!user.IsActive || !user.AwaitsActivation)
                {
                    return Expired();
                }

                challenge.MarkVerified(now, now + Limits.PasswordStepLifetime);
                if (!await TrySaveAsync(challenge, cancellationToken))
                {
                    continue;
                }

                return Activated(user);
            }

            challenge.RecordFailure();
            if (!await TrySaveAsync(challenge, cancellationToken))
            {
                continue;
            }

            if (user is not null)
            {
                await audit.WriteAsync(AuditActions.ActivationFailed, user.Id, user.FullName, $"Спроба {challenge.FailedAttempts}", cancellationToken);
            }

            var left = Limits.MaxFailedAttempts - challenge.FailedAttempts;
            return left <= 0
                ? Expired()
                : Error.Validation(Texts.T("Код неправильний. Залишилось спроб: {0}.", left), InvalidCode);
        }

        return Expired();
    }

    /// <summary>
    /// Sets the first password of the account whose code was entered and ends the activation.
    /// </summary>
    /// <param name="flow">The activation.</param>
    /// <param name="password">The new password (checked by the password policy).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated user, or an error.</returns>
    public async Task<Result<User>> CompleteAsync(ActivationFlow flow, string? password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(flow);
        var now = timeProvider.GetUtcNow();
        var challenge = await db.ActivationChallenges.FirstOrDefaultAsync(c => c.Id == flow.ChallengeId, cancellationToken);
        if (challenge is not { VerifiedAt: not null, UserId: { } userId } || !challenge.IsUsable(now, Limits.MaxFailedAttempts))
        {
            return Expired();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not { IsActive: true, AwaitsActivation: true })
        {
            return Expired();
        }

        // The owner proved the address; the password goes through every validator (length, breached passwords).
        user.EmailConfirmed = true;
        var result = await userManager.AddPasswordAsync(user, password ?? string.Empty);
        if (!result.Succeeded)
        {
            user.EmailConfirmed = false;
            return Error.Validation(string.Join(" ", result.Errors.Select(static e => e.Description)), "identity");
        }

        db.ActivationChallenges.RemoveRange(await db.ActivationChallenges.Where(c => c.EmailHash == challenge.EmailHash).ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(AuditActions.AccountActivated, user.Id, user.FullName, null, cancellationToken);
        return user;
    }

    /// <summary>
    /// Computes the stored form of an address: the hex SHA-256 of the normalized address.
    /// </summary>
    /// <param name="normalizedEmail">The normalized address.</param>
    /// <returns>The hash.</returns>
    internal static string HashEmail(string normalizedEmail)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));

    /// <summary>
    /// The error of a code that can no longer be entered.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error Expired()
        => Error.Validation(Texts.T("Код застарів або його вже використано. Надішліть новий код."), ExpiredCode);

    /// <summary>
    /// Describes the account whose code was entered.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The account.</returns>
    private static ActivatedAccount Activated(User user) => new(user.FirstName, user.TwoFactorEnabled, user.PhotoId is not null);

    /// <summary>
    /// Logs a code request for an address without an account waiting for activation (the address is not logged).
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "An activation code was requested for an address without an account waiting for activation; no e-mail was sent")]
    private static partial void LogNotEligible(ILogger logger);

    /// <summary>
    /// Creates a code request (or reuses the last one during the pause) and sends the code to an eligible account.
    /// </summary>
    /// <param name="email">The address typed by the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cookie value and the state, or an error.</returns>
    private async Task<Result<ActivationStart>> IssueAsync(string? email, CancellationToken cancellationToken)
    {
        var normalized = EmailAddress.Normalize(email);
        if (normalized is null)
        {
            return Error.Validation(Texts.T("Вкажіть коректну адресу електронної пошти."));
        }

        if (!outbox.IsEnabled)
        {
            return new Error("email_unavailable", Texts.T("Надсилання листів не налаштовано, тому активація зараз недоступна. Зверніться до адміністратора."), ErrorType.Unavailable);
        }

        var now = timeProvider.GetUtcNow();
        var hash = HashEmail(normalized);
        var hourAgo = now.AddHours(-1);
        var recent = await db.ActivationChallenges
            .Where(c => c.EmailHash == hash && c.CreatedAt > hourAgo)
            .OrderByDescending(static c => c.CreatedAt)
            .Select(static c => new { c.Id, c.CreatedAt, c.VerifiedAt })
            .ToListAsync(cancellationToken);

        // During the pause the last request stays: the same code still works and no new e-mail goes out.
        if (recent.Count > 0 && recent[0].VerifiedAt is null && recent[0].CreatedAt + Limits.ResendInterval > now)
        {
            return Started(recent[0].Id, normalized, ActivationStage.Code, ResendAfter(recent[0].CreatedAt, now));
        }

        if (recent.Count >= Limits.MaxCodesPerHour)
        {
            return new Error("too_many_codes", Texts.T("Забагато запитів коду для цієї адреси. Спробуйте пізніше."), ErrorType.TooManyRequests);
        }

        var user = await userManager.FindByEmailAsync(normalized);
        var eligible = user is { IsActive: true, AwaitsActivation: true };
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new ActivationChallenge(hash, eligible ? user!.Id : null, _codeProtector.Protect(Encoding.UTF8.GetBytes(code)), now, now + Limits.CodeLifetime);
        db.ActivationChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);

        if (eligible)
        {
            outbox.Enqueue(AccountEmails.ActivationCode(user!, code, Limits.CodeLifetime, emailOptions.Value.SiteUrl));
            await audit.WriteAsync(AuditActions.ActivationCodeSent, user!.Id, user.FullName, null, cancellationToken);
        }
        else
        {
            LogNotEligible(logger);
        }

        return Started(challenge.Id, normalized, ActivationStage.Code, ResendAfter(now, now));
    }

    /// <summary>
    /// Builds the answer to a code request.
    /// </summary>
    /// <param name="challengeId">The request.</param>
    /// <param name="email">The address.</param>
    /// <param name="stage">The stage.</param>
    /// <param name="resendAfter">Seconds until another code can be requested.</param>
    /// <returns>The answer.</returns>
    private ActivationStart Started(Guid challengeId, string email, ActivationStage stage, int resendAfter)
        => new(
            _ticketProtector.Protect(string.Create(CultureInfo.InvariantCulture, $"{challengeId:N}|{email}"), TicketLifetime),
            new ActivationStatus(stage, email, resendAfter));

    /// <summary>
    /// Computes the seconds until another code can be requested.
    /// </summary>
    /// <param name="requestedAt">The moment of the last request.</param>
    /// <param name="now">The current moment.</param>
    /// <returns>The seconds (0 — now).</returns>
    private int ResendAfter(DateTimeOffset requestedAt, DateTimeOffset now)
        => Math.Max(0, (int)Math.Ceiling((requestedAt + Limits.ResendInterval - now).TotalSeconds));

    /// <summary>
    /// Determines whether a newer code was requested for the same address (only the newest is valid).
    /// </summary>
    /// <param name="challenge">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when replaced.</returns>
    private Task<bool> IsReplacedAsync(ActivationChallenge challenge, CancellationToken cancellationToken)
        => db.ActivationChallenges.AnyAsync(c => c.EmailHash == challenge.EmailHash && c.CreatedAt > challenge.CreatedAt, cancellationToken);

    /// <summary>
    /// Compares a typed code with the stored one in constant time.
    /// </summary>
    /// <param name="challenge">The request.</param>
    /// <param name="digits">The typed digits.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    private bool Matches(ActivationChallenge challenge, string digits)
    {
        byte[] expected;
        try
        {
            expected = _codeProtector.Unprotect(challenge.ProtectedCode);
        }
        catch (CryptographicException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, Encoding.UTF8.GetBytes(digits));
    }

    /// <summary>
    /// Saves a change of a request; a parallel change of the same row makes the caller start over.
    /// </summary>
    /// <param name="challenge">The changed request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when saved.</returns>
    private async Task<bool> TrySaveAsync(ActivationChallenge challenge, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(challenge).State = EntityState.Detached;
            return false;
        }
    }
}
