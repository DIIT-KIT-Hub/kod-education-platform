namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// Limits of account activation (section <c>Authentication:Activation</c>). The defaults follow OWASP (single use,
/// short life, throttled guessing) and NIST SP 800-63B-4 for codes sent to an address of record (at least six random
/// digits).
/// </summary>
public sealed class ActivationOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Activation";

    /// <summary>
    /// Gets or sets how long a code can be entered.
    /// </summary>
    public TimeSpan CodeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets how long the owner has to choose the password after entering the right code.
    /// </summary>
    public TimeSpan PasswordStepLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets how many wrong codes end a code (a new one must be requested).
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the pause between two codes for one address.
    /// </summary>
    public TimeSpan ResendInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets how many codes one address can request per hour.
    /// </summary>
    public int MaxCodesPerHour { get; set; } = 5;

    /// <summary>
    /// Gets or sets the shortest time a code request takes, so a request for an existing account (which writes and sends
    /// more) cannot be told apart by its duration.
    /// </summary>
    public TimeSpan MinimumRequestDuration { get; set; } = TimeSpan.FromMilliseconds(500);
}
