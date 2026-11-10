using System.Globalization;
using System.Text;
using Npgsql;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Where the databases of organizations are: on the server of the master database, each under its own name
/// (<c>&lt;master&gt;_org_&lt;code&gt;</c>, fixed at creation). Npgsql keeps one small connection pool per database.
/// </summary>
/// <param name="masterConnectionString">The connection string of the master database.</param>
public sealed class OrganizationDatabases(string masterConnectionString)
{
    /// <summary>
    /// The largest pool of one organization (up to a hundred organizations share the server's connections).
    /// </summary>
    public const int MaxPoolSize = 10;

    /// <summary>
    /// Gets the connection string of an organization's database.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The connection string.</returns>
    public string ConnectionStringOf(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        return new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = organization.DatabaseName,
            MaxPoolSize = MaxPoolSize,
            ConnectionIdleLifetime = 60,
        }.ConnectionString;
    }

    /// <summary>
    /// Chooses the database name of a new organization: the master's name, <c>_org_</c> and the code in lower case
    /// (latin letters, digits and underscores), within PostgreSQL's 63 characters.
    /// </summary>
    /// <param name="code">The organization's code.</param>
    /// <returns>The database name.</returns>
    public string NameFor(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var master = new NpgsqlConnectionStringBuilder(masterConnectionString).Database ?? "odc";
        var slug = new StringBuilder();
        foreach (var c in code.ToLowerInvariant())
        {
            slug.Append(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? c : '_');
        }

        var name = string.Create(CultureInfo.InvariantCulture, $"{master}_org_{slug}");
        return name.Length <= Organization.DatabaseNameMaxLength ? name : name[..Organization.DatabaseNameMaxLength];
    }
}
