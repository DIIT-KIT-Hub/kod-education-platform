namespace KOD.Infrastructure.Identity;

/// <summary>
/// Password checks beyond the Identity policy (section <c>Authentication:Passwords</c>).
/// </summary>
public sealed class PasswordSecurityOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Passwords";

    /// <summary>
    /// Gets or sets the maximum password length (also bounds the hashing work per request).
    /// </summary>
    public int MaximumLength { get; set; } = 128;

    /// <summary>
    /// Gets or sets a value indicating whether passwords are checked against known data breaches.
    /// </summary>
    public bool CheckBreachedPasswords { get; set; } = true;

    /// <summary>
    /// Gets or sets the k-anonymity range API (the hash prefix is appended).
    /// </summary>
    public Uri BreachedPasswordsApi { get; set; } = new("https://api.pwnedpasswords.com/range/");
}
