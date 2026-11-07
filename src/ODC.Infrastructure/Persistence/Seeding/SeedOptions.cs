namespace ODC.Infrastructure.Persistence.Seeding;

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
    /// Gets or sets the e-mail of the first system administrator. The account is created without a password and activated
    /// from the sign-in page with a one-time code sent to this address, so it must be a real mailbox.
    /// </summary>
    public string AdminEmail { get; set; } = "admin@odc.local";

    /// <summary>
    /// Gets or sets a value indicating whether a sample structure, staff and news are created (for development and demos).
    /// </summary>
    public bool DemoData { get; set; }

    /// <summary>
    /// Gets or sets the password of demo accounts (development and demos only: they are created already activated, and still
    /// enroll an authenticator at the first sign-in). Demo accounts are not created when it is empty.
    /// </summary>
    public string? DemoPassword { get; set; }
}
