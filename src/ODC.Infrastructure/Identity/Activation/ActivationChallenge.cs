using ODC.Domain.Common;

namespace ODC.Infrastructure.Identity.Activation;

/// <summary>
/// One request of an activation code: the code sent to an address (kept encrypted), its lifetime, the failed attempts
/// and whether it was entered correctly. A row is written for every request, also for addresses without an account
/// waiting for activation (<see cref="UserId"/> is then <see langword="null"/> and no e-mail is sent), so the answers,
/// the limits and the timing are the same for every address and reveal nothing about which accounts exist.
/// </summary>
/// <remarks>
/// Only the newest request of an address is valid. Rows are deleted after the password is set and, at the latest, a day
/// after the request (they are counted for the hourly limit until then).
/// </remarks>
public sealed class ActivationChallenge : Entity
{
    /// <summary>
    /// Length of <see cref="EmailHash"/> (hex SHA-256).
    /// </summary>
    public const int EmailHashLength = 64;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivationChallenge"/> class.
    /// </summary>
    /// <param name="emailHash">The SHA-256 of the normalized address (the address itself is not stored).</param>
    /// <param name="userId">The account waiting for activation, or <see langword="null"/> when there is none.</param>
    /// <param name="protectedCode">The code, encrypted with Data Protection.</param>
    /// <param name="createdAt">The moment of the request.</param>
    /// <param name="expiresAt">The end of the code's lifetime.</param>
    public ActivationChallenge(string emailHash, Guid? userId, byte[] protectedCode, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        EmailHash = emailHash;
        UserId = userId;
        ProtectedCode = protectedCode;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivationChallenge"/> class for EF Core materialization.
    /// </summary>
    private ActivationChallenge()
    {
        EmailHash = string.Empty;
        ProtectedCode = [];
    }

    /// <summary>
    /// Gets the SHA-256 (hex) of the normalized address.
    /// </summary>
    public string EmailHash { get; private set; }

    /// <summary>
    /// Gets the account waiting for activation, or <see langword="null"/> for an address without one.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the code, encrypted with Data Protection (a database dump alone does not reveal it).
    /// </summary>
    public byte[] ProtectedCode { get; private set; }

    /// <summary>
    /// Gets the moment of the request.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the end of the code's lifetime or, once verified, of the time left to choose the password.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the number of wrong codes entered.
    /// </summary>
    public int FailedAttempts { get; private set; }

    /// <summary>
    /// Gets the moment the right code was entered, or <see langword="null"/>.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>
    /// Determines whether the code can still be entered or, once verified, the password chosen.
    /// </summary>
    /// <param name="now">The current moment.</param>
    /// <param name="maxFailedAttempts">The most wrong codes allowed.</param>
    /// <returns><see langword="true"/> while usable.</returns>
    public bool IsUsable(DateTimeOffset now, int maxFailedAttempts) => ExpiresAt > now && FailedAttempts < maxFailedAttempts;

    /// <summary>
    /// Counts a wrong code.
    /// </summary>
    public void RecordFailure() => FailedAttempts++;

    /// <summary>
    /// Marks the code as entered correctly and gives the owner time to choose the password.
    /// </summary>
    /// <param name="now">The current moment.</param>
    /// <param name="passwordDeadline">The end of the time to choose the password.</param>
    public void MarkVerified(DateTimeOffset now, DateTimeOffset passwordDeadline)
    {
        VerifiedAt = now;
        ExpiresAt = passwordDeadline;
    }
}
