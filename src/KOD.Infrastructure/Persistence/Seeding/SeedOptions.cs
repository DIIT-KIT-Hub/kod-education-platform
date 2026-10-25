namespace KOD.Infrastructure.Persistence.Seeding;

/// <summary>
/// Initial data settings (section <c>Seed</c>).
/// </summary>
public sealed class SeedOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Seed";

    /// <summary>
    /// Gets or sets the official name of the first organization, created when the structure is empty.
    /// </summary>
    public string OrganizationName { get; set; } = "Моя організація";

    /// <summary>
    /// Gets or sets the abbreviation of the first organization.
    /// </summary>
    public string OrganizationShortName { get; set; } = "МО";

    /// <summary>
    /// Gets or sets the code of the first organization.
    /// </summary>
    public string OrganizationCode { get; set; } = "ORG";

    /// <summary>
    /// Gets or sets the e-mail of the first system administrator.
    /// </summary>
    public string AdminEmail { get; set; } = "admin@kod.local";

    /// <summary>
    /// Gets or sets the initial administrator password. When empty, a random password is generated and written to the log once.
    /// The password must be changed at the first sign-in.
    /// </summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a sample structure, staff and news are created (for development and demos).
    /// </summary>
    public bool DemoData { get; set; }

    /// <summary>
    /// Gets or sets the password of demo accounts. Demo accounts are not created when it is empty.
    /// </summary>
    public string? DemoPassword { get; set; }
}
