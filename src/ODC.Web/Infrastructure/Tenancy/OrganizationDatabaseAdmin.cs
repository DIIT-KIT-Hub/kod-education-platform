using Microsoft.EntityFrameworkCore;
using Npgsql;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Creates, migrates, locks and drops the database of an organization and its PostgreSQL roles with the master's account
/// (it needs <c>CREATEDB</c> and <c>CREATEROLE</c>, never <c>SUPERUSER</c>). Every step can run again without harm, so a
/// start finishes an interrupted creation or deletion.
/// </summary>
/// <remarks>
/// The owner role (<c>&lt;database&gt;_owner</c>) owns the database and runs the migrations; the application role
/// (<c>&lt;database&gt;_app</c>) reads and writes the data of the tables the owner creates (default privileges), without
/// DDL. Nobody else may connect: <c>CONNECT</c> is revoked from <c>PUBLIC</c>.
/// </remarks>
/// <param name="databases">Where the databases are.</param>
/// <param name="secrets">Decrypts the passwords.</param>
public sealed class OrganizationDatabaseAdmin(OrganizationDatabases databases, OrganizationSecrets secrets)
{
    /// <summary>
    /// The most connections of the owner role (migrations and backups).
    /// </summary>
    private const int OwnerConnectionLimit = 4;

    /// <summary>
    /// The latest migration of an organization's database (the schema every organization must have to answer requests).
    /// </summary>
    /// <returns>The migration identifier.</returns>
    public static string LatestMigration()
    {
        using var db = new OrganizationDbContext(new DbContextOptionsBuilder<OrganizationDbContext>().UseNpgsql("Host=localhost").UseSnakeCaseNamingConvention().Options);
        return db.Database.GetMigrations().Last();
    }

    /// <summary>
    /// Creates the roles (or sets their passwords), the database and its privileges, whatever of it is missing.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task CreateAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var database = organization.DatabaseName;
        var owner = OrganizationDatabases.OwnerRole(database);
        var app = OrganizationDatabases.AppRole(database);
        var separate = organization.OwnerPassword is not null && organization.AppPassword is not null;

        await using (var connection = await OpenAsync(null, cancellationToken))
        {
            if (separate)
            {
                await EnsureRoleAsync(connection, owner, secrets.UnprotectPassword(organization.Id, organization.OwnerPassword!), OwnerConnectionLimit, cancellationToken);
                await EnsureRoleAsync(connection, app, secrets.UnprotectPassword(organization.Id, organization.AppPassword!), OrganizationDatabases.MaxPoolSize + 2, cancellationToken);

                // The master's account creates the database for the owner and sets the owner's default privileges: it has to
                // be able to act as the owner (a superuser always can).
                if (!await ScalarAsync<bool>(connection, "SELECT pg_has_role(current_user, @role, 'USAGE')", cancellationToken, ("role", owner)))
                {
                    await ExecuteAsync(connection, $"GRANT {Quote(owner)} TO CURRENT_USER", cancellationToken);
                }
            }

            if (!await ScalarAsync<bool>(connection, "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", cancellationToken, ("name", database)))
            {
                await ExecuteAsync(connection, separate ? $"CREATE DATABASE {Quote(database)} OWNER {Quote(owner)}" : $"CREATE DATABASE {Quote(database)}", cancellationToken);
            }

            if (separate)
            {
                await ExecuteAsync(
                    connection,
                    $"REVOKE ALL ON DATABASE {Quote(database)} FROM PUBLIC; GRANT CONNECT, TEMPORARY ON DATABASE {Quote(database)} TO {Quote(owner)}; GRANT CONNECT ON DATABASE {Quote(database)} TO {Quote(app)}",
                    cancellationToken);
            }
        }

        if (separate)
        {
            await using var inside = await OpenAsync(database, cancellationToken);
            await ExecuteAsync(
                inside,
                $"""
                REVOKE ALL ON SCHEMA public FROM PUBLIC;
                GRANT USAGE, CREATE ON SCHEMA public TO {Quote(owner)};
                GRANT USAGE ON SCHEMA public TO {Quote(app)};
                ALTER DEFAULT PRIVILEGES FOR ROLE {Quote(owner)} IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {Quote(app)};
                ALTER DEFAULT PRIVILEGES FOR ROLE {Quote(owner)} IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO {Quote(app)};
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {Quote(app)};
                GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO {Quote(app)};
                """,
                cancellationToken);
        }
    }

    /// <summary>
    /// Brings the organization's database to the latest migration as its owner.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The last applied migration.</returns>
    public async Task<string> MigrateAsync(Organization organization, CancellationToken cancellationToken)
    {
        await using var db = OwnerContext(organization);
        await db.Database.MigrateAsync(cancellationToken);
        return (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Last();
    }

    /// <summary>
    /// Lets the application role connect again (after a suspension).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task UnlockAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (organization.AppPassword is null)
        {
            return;
        }

        await using var connection = await OpenAsync(null, cancellationToken);
        await ExecuteAsync(connection, $"ALTER ROLE {Quote(OrganizationDatabases.AppRole(organization.DatabaseName))} LOGIN", cancellationToken);
    }

    /// <summary>
    /// Stops the application role from connecting and ends its connections (a suspension).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task LockAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (organization.AppPassword is null)
        {
            return;
        }

        var app = OrganizationDatabases.AppRole(organization.DatabaseName);
        await using var connection = await OpenAsync(null, cancellationToken);
        await ExecuteAsync(connection, $"ALTER ROLE {Quote(app)} NOLOGIN", cancellationToken);
        await ExecuteAsync(connection, "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE usename = @role", cancellationToken, ("role", app));
        NpgsqlConnection.ClearAllPools();
    }

    /// <summary>
    /// Sets new passwords of the roles.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="ownerPassword">The owner's password.</param>
    /// <param name="appPassword">The application role's password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task ChangePasswordsAsync(Organization organization, string ownerPassword, string appPassword, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        await using var connection = await OpenAsync(null, cancellationToken);
        await ExecuteAsync(connection, $"ALTER ROLE {Quote(OrganizationDatabases.OwnerRole(organization.DatabaseName))} PASSWORD {Literal(ownerPassword)}", cancellationToken);
        await ExecuteAsync(connection, $"ALTER ROLE {Quote(OrganizationDatabases.AppRole(organization.DatabaseName))} PASSWORD {Literal(appPassword)}", cancellationToken);
    }

    /// <summary>
    /// Drops the organization's database (ending its connections) and its roles, whatever of them exists.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task DropAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        NpgsqlConnection.ClearAllPools();
        await using var connection = await OpenAsync(null, cancellationToken);
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {Quote(organization.DatabaseName)} WITH (FORCE)", cancellationToken);
        if (databases.SeparateRoles || organization.OwnerPassword is not null)
        {
            await ExecuteAsync(connection, $"DROP ROLE IF EXISTS {Quote(OrganizationDatabases.AppRole(organization.DatabaseName))}", cancellationToken);
            await ExecuteAsync(connection, $"DROP ROLE IF EXISTS {Quote(OrganizationDatabases.OwnerRole(organization.DatabaseName))}", cancellationToken);
        }
    }

    /// <summary>
    /// Drops the organization's database only (before a restore; the roles stay).
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task DropDatabaseAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        NpgsqlConnection.ClearAllPools();
        await using var connection = await OpenAsync(null, cancellationToken);
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {Quote(organization.DatabaseName)} WITH (FORCE)", cancellationToken);
    }

    /// <summary>
    /// A context of the organization's database opened as its owner.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The context (the caller disposes it).</returns>
    public OrganizationDbContext OwnerContext(Organization organization)
        => new(new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(databases.OwnerConnectionStringOf(organization), static npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention()
            .Options);

    /// <summary>
    /// Quotes an identifier.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The quoted identifier.</returns>
    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    /// <summary>
    /// Quotes a string literal (role passwords cannot be parameters).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The literal.</returns>
    private static string Literal(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>
    /// Creates a role or sets its password and attributes.
    /// </summary>
    /// <param name="connection">The master's connection.</param>
    /// <param name="role">The role.</param>
    /// <param name="password">Its password.</param>
    /// <param name="connectionLimit">The most connections.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    private static async Task EnsureRoleAsync(NpgsqlConnection connection, string role, string password, int connectionLimit, CancellationToken cancellationToken)
    {
        var exists = await ScalarAsync<bool>(connection, "SELECT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = @role)", cancellationToken, ("role", role));
        var attributes = $"LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS CONNECTION LIMIT {connectionLimit} PASSWORD {Literal(password)}";
        await ExecuteAsync(connection, exists ? $"ALTER ROLE {Quote(role)} WITH {attributes}" : $"CREATE ROLE {Quote(role)} WITH {attributes}", cancellationToken);
    }

    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="parameters">Its parameters.</param>
    /// <returns>A task.</returns>
    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="parameters">Its parameters.</param>
    /// <returns>The value.</returns>
    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>
    /// Opens a connection with the master's account.
    /// </summary>
    /// <param name="database">The database (null: the master database).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The open connection.</returns>
    private async Task<NpgsqlConnection> OpenAsync(string? database, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(databases.AdministrationConnectionString(database));
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
