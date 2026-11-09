using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ODC.Web.Infrastructure.Persistence;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// A throwaway PostgreSQL database for the tests that need the real provider (complex properties, <c>xmin</c>
/// concurrency, SQL translation): created from the model for one test class and dropped afterwards. The server comes
/// from <c>ODC_TEST_DATABASE</c> (a connection string; CI) or from the developer's user secrets of ODC.Web
/// (<c>ConnectionStrings:Database</c>); only the database name is replaced, so the developer's data is never touched.
/// Without either the tests skip.
/// </summary>
public sealed class PostgresDatabase : IAsyncLifetime
{
    /// <summary>
    /// The user-secrets id of ODC.Web.
    /// </summary>
    private const string WebSecretsId = "odc-web-3b8f9c52-6a41-4e0b-9d7e-2f5c1a7e8d10";

    /// <summary>
    /// The connection string of the test database, or null when no server is configured.
    /// </summary>
    private readonly string? _connectionString;

    /// <summary>
    /// The tables of the model (emptied between tests).
    /// </summary>
    private string[] _tables = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDatabase"/> class.
    /// </summary>
    public PostgresDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("ODC_TEST_DATABASE")
            ?? new ConfigurationBuilder().AddUserSecrets(WebSecretsId).Build().GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _connectionString = new NpgsqlConnectionStringBuilder(configured) { Database = $"odc_tests_{Guid.NewGuid():N}", Options = "-c synchronous_commit=off" }.ConnectionString;
        }
    }

    /// <summary>
    /// Gets the options of a context on the test database.
    /// </summary>
    public DbContextOptions<ApplicationDbContext> Options => field ??= new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(_connectionString)
        .UseSnakeCaseNamingConvention()
        .Options;

    /// <summary>
    /// Skips the current test when no PostgreSQL server is configured.
    /// </summary>
    public void RequireServer()
        => Assert.SkipWhen(_connectionString is null, "No PostgreSQL server: set ODC_TEST_DATABASE or the user secrets of ODC.Web.");

    /// <summary>
    /// Empties every table (each test starts from a clean database).
    /// </summary>
    public void Reset()
    {
        RequireServer();
        using var db = new ApplicationDbContext(Options);

        // Deleting the few rows of a test is much faster than TRUNCATE (which rewrites every table). The table names come
        // from the model, never from input; they are ordered so dependants go first.
        var sql = string.Concat(_tables.Select(static t => $"DELETE FROM {t};"));
        db.Database.ExecuteSqlRaw(sql);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using var db = new ApplicationDbContext(Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        _tables = DependantsFirst(db.Model).Select(static t => $"\"{t}\"").ToArray();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        // Pooled connections keep the database open: close them and force the drop (PostgreSQL 13+).
        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var name = builder.Database;
        builder.Database = "postgres";
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    /// <summary>
    /// Orders the tables so that every table comes before the tables it references (rows can be deleted in this order).
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The table names.</returns>
    private static List<string> DependantsFirst(Microsoft.EntityFrameworkCore.Metadata.IModel model)
    {
        var types = model.GetEntityTypes().Where(static t => t.GetTableName() is not null && !t.IsOwned()).ToList();
        var references = types.ToDictionary(
            static t => t.GetTableName()!,
            t => t.GetForeignKeys().Select(static k => k.PrincipalEntityType.GetTableName()).OfType<string>().Where(name => name != t.GetTableName()).ToHashSet(),
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
