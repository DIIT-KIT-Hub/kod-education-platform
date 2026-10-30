namespace KOD.Infrastructure.Identity;

/// <summary>
/// Lifetime of a sign-in, enforced by its refresh token family (section <c>Authentication:Session</c>). The defaults
/// follow NIST SP 800-63B-4 for AAL2 (password + authenticator app), and the absolute lifetime even its AAL3 limit.
/// </summary>
public sealed class SessionLifetimeOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Session";

    /// <summary>
    /// Gets or sets how long a sign-in survives without requests (sliding with every refresh): NIST SP 800-63B-4, AAL2,
    /// "SHOULD be no more than 1 hour".
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the absolute lifetime of a sign-in: after it the user signs in again even when active (NIST SP
    /// 800-63B-4: AAL2 at most 24 hours, AAL3 at most 12 hours).
    /// </summary>
    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromHours(12);
}
