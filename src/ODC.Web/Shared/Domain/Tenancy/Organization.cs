using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Shared.Domain.Tenancy;

/// <summary>
/// An organization of the cabinet in the registry of the master database: where its own database is, its state, the
/// version of its schema and its secrets (the passwords of its PostgreSQL roles and the key of its files, both encrypted
/// with Data Protection, so the master database alone opens nothing). The organization's structure, people and work
/// live in that database; its root unit there has the same <see cref="Id"/>.
/// </summary>
public sealed class Organization : IAuditable
{
    /// <summary>
    /// Maximum length of <see cref="DatabaseName"/>: PostgreSQL identifiers have 63 characters, and the names of the
    /// database's roles add <c>_owner</c>.
    /// </summary>
    public const int DatabaseNameMaxLength = 57;

    /// <summary>
    /// Maximum length of <see cref="SchemaVersion"/> (an EF Core migration identifier).
    /// </summary>
    public const int SchemaVersionMaxLength = 150;

    /// <summary>
    /// Initializes a new instance of the <see cref="Organization"/> class (an active organization).
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
    /// Gets the state.
    /// </summary>
    public OrganizationState State { get; private set; } = OrganizationState.Active;

    /// <summary>
    /// Gets a value indicating whether the organization works (only an active one answers requests).
    /// </summary>
    public bool IsActive => State == OrganizationState.Active;

    /// <summary>
    /// Gets the last migration applied to the organization's database (null before the first one).
    /// </summary>
    public string? SchemaVersion { get; private set; }

    /// <summary>
    /// Gets when the database was last brought to <see cref="SchemaVersion"/>.
    /// </summary>
    public DateTimeOffset? MigratedAt { get; private set; }

    /// <summary>
    /// Gets the password of the role that owns the database and runs the migrations, encrypted (null when the
    /// organization uses the master's credentials).
    /// </summary>
    public string? OwnerPassword { get; private set; }

    /// <summary>
    /// Gets the password of the role the application works with (data only, no DDL), encrypted (null when the
    /// organization uses the master's credentials).
    /// </summary>
    public string? AppPassword { get; private set; }

    /// <summary>
    /// Gets the key of the organization's files (AES-256), encrypted.
    /// </summary>
    public string? FileKey { get; private set; }

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
    /// Starts the creation: the row is saved before the roles and the database exist.
    /// </summary>
    /// <param name="ownerPassword">The encrypted password of the owner role (null: the master's credentials).</param>
    /// <param name="appPassword">The encrypted password of the application role (null: the master's credentials).</param>
    /// <param name="fileKey">The encrypted key of the files.</param>
    public void BeginCreation(string? ownerPassword, string? appPassword, string fileKey)
    {
        State = OrganizationState.Creating;
        OwnerPassword = ownerPassword;
        AppPassword = appPassword;
        ArgumentException.ThrowIfNullOrEmpty(fileKey);
        FileKey = fileKey;
    }

    /// <summary>
    /// Gives an organization created before it had a key of its files one.
    /// </summary>
    /// <param name="fileKey">The encrypted key.</param>
    public void KeepFilesWith(string fileKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileKey);
        FileKey ??= fileKey;
    }

    /// <summary>
    /// Replaces the passwords of the roles (rotation).
    /// </summary>
    /// <param name="ownerPassword">The encrypted password of the owner role.</param>
    /// <param name="appPassword">The encrypted password of the application role.</param>
    public void ChangePasswords(string ownerPassword, string appPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerPassword);
        ArgumentException.ThrowIfNullOrEmpty(appPassword);
        OwnerPassword = ownerPassword;
        AppPassword = appPassword;
    }

    /// <summary>
    /// Records that the database was brought to a migration.
    /// </summary>
    /// <param name="schemaVersion">The last applied migration.</param>
    /// <param name="at">When.</param>
    public void Migrated(string schemaVersion, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaVersion);
        SchemaVersion = schemaVersion.Length <= SchemaVersionMaxLength ? schemaVersion : throw new ArgumentException("The schema version is too long.", nameof(schemaVersion));
        MigratedAt = at;
    }

    /// <summary>
    /// Opens the organization for requests (after its creation or a suspension).
    /// </summary>
    public void Activate()
    {
        if (State == OrganizationState.Deleting)
        {
            throw new DomainException(Texts.T("Організацію вже видаляють."));
        }

        State = OrganizationState.Active;
    }

    /// <summary>
    /// Suspends the organization: no requests, the data stays.
    /// </summary>
    public void Suspend()
    {
        if (State != OrganizationState.Active)
        {
            throw new DomainException(Texts.T("Призупинити можна лише активну організацію."));
        }

        State = OrganizationState.Suspended;
    }

    /// <summary>
    /// Starts the deletion (only of a suspended organization).
    /// </summary>
    public void BeginDeletion()
    {
        if (State is not (OrganizationState.Suspended or OrganizationState.Deleting or OrganizationState.Creating))
        {
            throw new DomainException(Texts.T("Спершу призупиніть організацію."));
        }

        State = OrganizationState.Deleting;
    }
}
