using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Shared.Domain.Tenancy;

/// <summary>
/// An organization of the cabinet in the registry of the master database: where its own database is. The organization's
/// structure, people and work live in that database; its root unit there has the same <see cref="Id"/>.
/// </summary>
public sealed class Organization : IAuditable
{
    /// <summary>
    /// Maximum length of <see cref="DatabaseName"/> (PostgreSQL identifiers).
    /// </summary>
    public const int DatabaseNameMaxLength = 63;

    /// <summary>
    /// Initializes a new instance of the <see cref="Organization"/> class.
    /// </summary>
    /// <param name="id">The identifier (the root unit's).</param>
    /// <param name="code">The code (the root unit's).</param>
    /// <param name="name">The name (the root unit's).</param>
    /// <param name="databaseName">The name of the organization's database.</param>
    /// <param name="shortName">The abbreviation (the root unit's).</param>
    public Organization(Guid id, string code, string name, string databaseName, string? shortName = null)
    {
        Id = id;
        Code = Guard.Code(code, OrganizationUnit.CodeMaxLength);
        Name = Guard.Text(name, OrganizationUnit.NameMaxLength, "Назва");
        ShortName = Guard.OptionalText(shortName, OrganizationUnit.ShortNameMaxLength, "Скорочена назва");
        DatabaseName = Guard.Text(databaseName, DatabaseNameMaxLength, "База даних");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Organization"/> class for EF Core materialization.
    /// </summary>
    private Organization()
    {
        Code = string.Empty;
        Name = string.Empty;
        DatabaseName = string.Empty;
    }

    /// <summary>
    /// Gets the identifier (equal to the root unit in the organization's database).
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the code (shown in addresses: <c>/o/{code}</c>).
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the abbreviation (shown where the organization must stay short: the switcher, list labels).
    /// </summary>
    public string? ShortName { get; private set; }

    /// <summary>
    /// Gets the name of the organization's database (fixed at creation: renaming the organization keeps it).
    /// </summary>
    public string DatabaseName { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the organization is active (an inactive one answers no requests).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Follows the code and names of the root unit.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="name">The name.</param>
    /// <param name="shortName">The abbreviation.</param>
    public void Rename(string code, string name, string? shortName)
    {
        Code = Guard.Code(code, OrganizationUnit.CodeMaxLength);
        Name = Guard.Text(name, OrganizationUnit.NameMaxLength, "Назва");
        ShortName = Guard.OptionalText(shortName, OrganizationUnit.ShortNameMaxLength, "Скорочена назва");
    }

    /// <summary>
    /// Activates or suspends the organization.
    /// </summary>
    /// <param name="isActive">The new state.</param>
    public void SetActive(bool isActive) => IsActive = isActive;
}
