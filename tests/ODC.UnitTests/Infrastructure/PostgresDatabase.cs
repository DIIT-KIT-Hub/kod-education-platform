using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ODC.Web.Infrastructure.Persistence;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// Throwaway PostgreSQL databases for the tests that need the real provider (complex properties, <c>xmin</c>
/// concurrency, SQL translation): a master database and an organizations' database, created from the models for one
/// test class and dropped afterwards. The server comes from <c>ODC_TEST_DATABASE</c> (a connection string; CI) or from
/// the developer's user secrets of ODC.Web (<c>ConnectionStrings:Database</c>); only the database names are replaced, so
/// the developer's data is never touched. Without either the tests skip.
/// </summary>
public sealed class PostgresDatabase : IAsyncLifetime
{
    /// <summary>
    /// The user-secrets id of ODC.Web.
    /// </summary>
    private const string WebSecretsId = "odc-web-3b8f9c52-6a41-4e0b-9d7e-2f5c1a7e8d10";

    /// <summary>
    /// The connection string of the master test database, or null when no server is configured.
    /// </summary>
    private readonly string? _masterConnectionString;

    /// <summary>
    /// The connection string of the organizations' test database, or null when no server is configured.
    /// </summary>
    private readonly string? _organizationConnectionString;

    /// <summary>
    /// The tables of the master model (emptied between tests).
    /// </summary>
    private string[] _masterTables = [];

    /// <summary>
    /// The tables of the organization model (emptied between tests).
    /// </summary>
    private string[] _organizationTables = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDatabase"/> class.
    /// </summary>
    public PostgresDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("ODC_TEST_DATABASE")
            ?? new ConfigurationBuilder().AddUserSecrets(WebSecretsId).Build().GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var name = $"odc_tests_{Guid.NewGuid():N}";
            _masterConnectionString = new NpgsqlConnectionStringBuilder(configured) { Database = name, Options = "-c synchronous_commit=off" }.ConnectionString;
            _organizationConnectionString = new NpgsqlConnectionStringBuilder(configured) { Database = name + "_org", Options = "-c synchronous_commit=off" }.ConnectionString;
        }
    }

    /// <summary>
    /// Gets the options of a context on the master test database.
    /// </summary>
    public DbContextOptions<MasterDbContext> MasterOptions => field ??= new DbContextOptionsBuilder<MasterDbContext>()
        .UseNpgsql(_masterConnectionString)
        .UseSnakeCaseNamingConvention()
        .Options;

    /// <summary>
    /// Gets the options of a context on the organizations' test database.
    /// </summary>
    public DbContextOptions<OrganizationDbContext> OrganizationOptions => field ??= new DbContextOptionsBuilder<OrganizationDbContext>()
        .UseNpgsql(_organizationConnectionString)
        .UseSnakeCaseNamingConvention()
        .Options;

    /// <summary>
    /// Skips the current test when no PostgreSQL server is configured.
    /// </summary>
    public void RequireServer()
        => Assert.SkipWhen(_masterConnectionString is null, "No PostgreSQL server: set ODC_TEST_DATABASE or the user secrets of ODC.Web.");

    /// <summary>
    /// Empties every table of both databases (each test starts from clean databases).
    /// </summary>
    public void Reset()
    {
        RequireServer();

        // Deleting the few rows of a test is much faster than TRUNCATE (which rewrites every table). The table names come
        // from the models, never from input; they are ordered so dependants go first.
        using (var master = new MasterDbContext(MasterOptions))
        {
            master.Database.ExecuteSqlRaw(string.Concat(_masterTables.Select(static t => $"DELETE FROM {t};")));
        }

        using var organization = new OrganizationDbContext(OrganizationOptions);
        organization.Database.ExecuteSqlRaw(string.Concat(_organizationTables.Select(static t => $"DELETE FROM {t};")));
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (_masterConnectionString is null)
        {
            return;
        }

        await using (var master = new MasterDbContext(MasterOptions))
        {
            await master.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            _masterTables = Quoted(DependantsFirst(master.Model));
        }

        await using var organization = new OrganizationDbContext(OrganizationOptions);
        await organization.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        _organizationTables = Quoted(DependantsFirst(organization.Model));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_masterConnectionString is null || _organizationConnectionString is null)
        {
            return;
        }

        // Pooled connections keep the databases open: close them and force the drop (PostgreSQL 13+).
        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(_masterConnectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        foreach (var name in new[] { new NpgsqlConnectionStringBuilder(_masterConnectionString).Database, new NpgsqlConnectionStringBuilder(_organizationConnectionString).Database })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Quotes table names for SQL.
    /// </summary>
    /// <param name="tables">The names.</param>
    /// <returns>The quoted names.</returns>
    private static string[] Quoted(List<string> tables) => [.. tables.Select(static t => $"\"{t}\"")];

    /// <summary>
    /// Orders the tables so that every table comes before the tables it references (rows can be deleted in this order).
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The table names.</returns>
    private static List<string> DependantsFirst(Microsoft.EntityFrameworkCore.Metadata.IModel model)
    {
        var types = model.GetEntityTypes().Where(static t => t.GetTableName() is not null && !t.IsOwned()).ToList();
        var references = types
            .GroupBy(static t => t.GetTableName()!, StringComparer.Ordinal)
            .ToDictionary(
                static g => g.Key,
                g => g.SelectMany(static t => t.GetForeignKeys()).Select(static k => k.PrincipalEntityType.GetTableName()).OfType<string>().Where(name => name != g.Key).ToHashSet(),
                StringComparer.Ordinal);
        var ordered = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        void Visit(string table)
        {
            if (!visited.Add(table))
            {
                return;
            }

            foreach (var dependant in references.Where(r => r.Value.Contains(table)).Select(static r => r.Key))
            {
                Visit(dependant);
            }

            ordered.Add(table);
        }

        foreach (var table in references.Keys)
        {
            Visit(table);
        }

        return ordered;
    }
}
