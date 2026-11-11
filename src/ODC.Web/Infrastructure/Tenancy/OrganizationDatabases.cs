using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Npgsql;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Where the databases of organizations are and how they are opened: on the server of the master database, each under
/// its own name (<c>&lt;master&gt;_org_&lt;code&gt;</c>, fixed at creation) and, with
/// <see cref="OrganizationOptions.SeparateRoles"/>, with roles of its own: <c>&lt;database&gt;_app</c> for the requests
/// (data only) and <c>&lt;database&gt;_owner</c> for the migrations. Npgsql keeps one small pool per connection string.
/// </summary>
/// <param name="masterConnectionString">The connection string of the master database (its account creates databases and roles).</param>
/// <param name="secrets">Decrypts the passwords of the roles.</param>
/// <param name="options">The settings.</param>
public sealed class OrganizationDatabases(string masterConnectionString, OrganizationSecrets secrets, IOptions<OrganizationOptions> options)
{
    /// <summary>
    /// The largest pool of one organization (up to a hundred organizations share the server's connections).
    /// </summary>
    public const int MaxPoolSize = 10;

    /// <summary>
    /// Gets a value indicating whether databases get roles of their own.
    /// </summary>
    public bool SeparateRoles => options.Value.SeparateRoles;

    /// <summary>
    /// Gets the name of the master database.
    /// </summary>
    public string MasterDatabase => new NpgsqlConnectionStringBuilder(masterConnectionString).Database ?? "odc";

    /// <summary>
    /// The role that owns an organization's database.
    /// </summary>
    /// <param name="databaseName">The database.</param>
    /// <returns>The role name.</returns>
    public static string OwnerRole(string databaseName) => databaseName + "_owner";

    /// <summary>
    /// The role the application works with in an organization's database.
    /// </summary>
    /// <param name="databaseName">The database.</param>
    /// <returns>The role name.</returns>
    public static string AppRole(string databaseName) => databaseName + "_app";

    /// <summary>
    /// Gets the connection string of an organization's database for the requests (the application role).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The connection string.</returns>
    public string ConnectionStringOf(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var builder = new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = organization.DatabaseName,
            MaxPoolSize = MaxPoolSize,
            ConnectionIdleLifetime = 60,
        };
        if (organization.AppPassword is { } password)
        {
            builder.Username = AppRole(organization.DatabaseName);
            builder.Password = secrets.UnprotectPassword(organization.Id, password);
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Gets the connection string of an organization's database for its migrations and backups (the owner role; no pool).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The connection string.</returns>
    public string OwnerConnectionStringOf(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var builder = new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = organization.DatabaseName,
            Pooling = false,
        };
        if (organization.OwnerPassword is { } password)
        {
            builder.Username = OwnerRole(organization.DatabaseName);
            builder.Password = secrets.UnprotectPassword(organization.Id, password);
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Gets a connection string with the master's account (which creates and drops databases and roles).
    /// </summary>
    /// <param name="database">The database to connect to (null: the master database).</param>
    /// <returns>The connection string.</returns>
    public string AdministrationConnectionString(string? database = null)
        => new NpgsqlConnectionStringBuilder(masterConnectionString) { Database = database ?? MasterDatabase, Pooling = false }.ConnectionString;

    /// <summary>
    /// Chooses the database name of a new organization: the master's name, <c>_org_</c> and the code in lower case
    /// (latin letters, digits and underscores), short enough for the names of its roles.
    /// </summary>
    /// <param name="code">The organization's code.</param>
    /// <returns>The database name.</returns>
    public string NameFor(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var slug = new StringBuilder();
        foreach (var c in code.ToLowerInvariant())
        {
            slug.Append(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? c : '_');
        }

        var name = string.Create(CultureInfo.InvariantCulture, $"{MasterDatabase}_org_{slug}");
        return name.Length <= Organization.DatabaseNameMaxLength ? name : name[..Organization.DatabaseNameMaxLength];
    }
}
