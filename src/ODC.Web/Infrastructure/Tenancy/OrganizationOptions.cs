namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Settings of the organizations' databases (section <c>Organizations</c>).
/// </summary>
public sealed class OrganizationOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Organizations";

    /// <summary>
    /// Gets or sets a value indicating whether every organization's database gets PostgreSQL roles of its own (an owner
    /// for the migrations and an application role without DDL, both with random passwords). The account of the master
    /// connection then needs <c>CREATEDB</c> and <c>CREATEROLE</c>. Off: every database is used with the master's
    /// credentials (a server where roles cannot be created).
    /// </summary>
    public bool SeparateRoles { get; set; } = true;

    /// <summary>
    /// Gets or sets how many organizations' databases are migrated at once at the start.
    /// </summary>
    public int MigrationParallelism { get; set; } = 4;

    /// <summary>
    /// Gets or sets the folder of <c>pg_dump</c> and <c>pg_restore</c> (empty: the system path).
    /// </summary>
    public string PostgresTools { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets where backups are written (relative to the content root).
    /// </summary>
    public string BackupRoot { get; set; } = "App_Data/backups";
}
