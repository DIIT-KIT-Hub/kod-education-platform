using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The schema every organization's database must have to answer requests: the latest migration of
/// <see cref="Persistence.OrganizationDbContext"/>. An organization whose migration failed answers 503 until a start
/// (or <c>migrate</c>) brings it up to date.
/// </summary>
public sealed class OrganizationSchema
{
    /// <summary>
    /// The latest migration, read once.
    /// </summary>
    private readonly Lazy<string> _latest = new(OrganizationDatabaseAdmin.LatestMigration);

    /// <summary>
    /// Gets the latest migration.
    /// </summary>
    public string Latest => _latest.Value;

    /// <summary>
    /// Whether an organization's database has the latest schema.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>Whether it may answer requests.</returns>
    public bool IsCurrent(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        return string.Equals(organization.SchemaVersion, Latest, StringComparison.Ordinal);
    }
}
