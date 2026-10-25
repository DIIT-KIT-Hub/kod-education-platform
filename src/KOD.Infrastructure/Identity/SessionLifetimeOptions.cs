namespace KOD.Infrastructure.Identity;

/// <summary>
/// Lifetime of authentication sessions (section <c>Authentication:Session</c>).
/// </summary>
public sealed class SessionLifetimeOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Session";

    /// <summary>
    /// Gets or sets how long a session survives without requests (sliding expiration).
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Gets or sets the absolute lifetime of a session: after it the user signs in again even when active
    /// (OWASP ASVS 3.3.2, level 2: at most 12 hours).
    /// </summary>
    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromHours(12);
}
