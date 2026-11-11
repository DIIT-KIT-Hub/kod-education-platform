namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// What a backup holds.
/// </summary>
/// <param name="Format">The version of the backup format.</param>
/// <param name="OrganizationId">The organization.</param>
/// <param name="Code">Its code then.</param>
/// <param name="Name">Its name then.</param>
/// <param name="DatabaseName">Its database.</param>
/// <param name="SchemaVersion">The last migration of the database.</param>
/// <param name="CreatedAt">When the backup was made.</param>
public sealed record BackupManifest(int Format, Guid OrganizationId, string Code, string Name, string DatabaseName, string? SchemaVersion, DateTimeOffset CreatedAt);
