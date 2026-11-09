using Npgsql;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// Where the organizations' database is: <c>ConnectionStrings:OrganizationDatabase</c>, or else the master database's
/// server and credentials with the database name <c>&lt;master&gt;_org</c>. Until there is a database per organization
/// (phase 3) every organization shares this one.
/// </summary>
public static class OrganizationDatabase
{
    /// <summary>
    /// The suffix of the organizations' database name.
    /// </summary>
    public const string NameSuffix = "_org";

    /// <summary>
    /// Resolves the connection string of the organizations' database.
    /// </summary>
    /// <param name="configured">The configured <c>ConnectionStrings:OrganizationDatabase</c>, if any.</param>
    /// <param name="master">The connection string of the master database.</param>
    /// <returns>The connection string.</returns>
    public static string ConnectionString(string? configured, string master)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var builder = new NpgsqlConnectionStringBuilder(master);
        builder.Database = (builder.Database ?? "odc") + NameSuffix;
        return builder.ConnectionString;
    }
}
