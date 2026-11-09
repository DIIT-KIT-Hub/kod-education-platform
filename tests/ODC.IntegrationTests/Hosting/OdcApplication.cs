using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ODC.IntegrationTests.Clients;
using ODC.Web.Infrastructure.Persistence;

namespace ODC.IntegrationTests.Hosting;

/// <summary>
/// The whole cabinet in memory (the real <c>Program</c>: migrations, seeding, middleware, endpoints, background services)
/// on a throwaway PostgreSQL database, with its own JWT signing key, e-mails written to a folder, a movable clock and no
/// calls to the internet. The server comes from <c>ODC_TEST_DATABASE</c> (a connection string; CI) or from the developer's
/// user secrets of ODC.Web (<c>ConnectionStrings:Database</c>); only the database name is replaced, so the developer's data
/// is never touched. Without either the tests skip.
/// </summary>
public sealed class OdcApplication : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// The e-mail of the first system administrator (created without a password, activated by e-mail).
    /// </summary>
    public const string AdminEmail = "admin@odc.local";

    /// <summary>
    /// The user-secrets id of ODC.Web.
    /// </summary>
    private const string WebSecretsId = "odc-web-3b8f9c52-6a41-4e0b-9d7e-2f5c1a7e8d10";

    /// <summary>
    /// The connection string of the test database, or null when no server is configured.
    /// </summary>
    private readonly string? _connectionString;

    /// <summary>
    /// The temporary folder of this run: e-mails, files, keys, logs and an empty client.
    /// </summary>
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"odc-it-{Guid.NewGuid():N}");

    /// <summary>
    /// The last remote address handed to a client.
    /// </summary>
    private int _lastAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="OdcApplication"/> class.
    /// </summary>
    public OdcApplication()
    {
        var configured = Environment.GetEnvironmentVariable("ODC_TEST_DATABASE")
            ?? new ConfigurationBuilder().AddUserSecrets(WebSecretsId).Build().GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _connectionString = new NpgsqlConnectionStringBuilder(configured)
            {
                Database = $"odc_it_{Guid.NewGuid():N}",
                Options = "-c synchronous_commit=off",
            }.ConnectionString;
        }

        // Upper and lower case, digits and more than five distinct characters, as the password rules require.
        DemoPassword = $"Demo-{Guid.NewGuid():N}"[..20] + "Aa7";
        Mailbox = new Mailbox(Path.Combine(_folder, "mail"));
        Accounts = new Accounts(this);
    }

    /// <summary>
    /// Gets the clock of the application.
    /// </summary>
    public TestClock Clock { get; } = new();

    /// <summary>
    /// Gets the folder the application writes its e-mails to.
    /// </summary>
    public Mailbox Mailbox { get; }

    /// <summary>
    /// Gets the sign-in helpers (authenticator keys, activation, photos).
    /// </summary>
    public Accounts Accounts { get; }

    /// <summary>
    /// Gets the password of the demo accounts (<c>Seed:DemoPassword</c>), different on every run.
    /// </summary>
    public string DemoPassword { get; }

    /// <summary>
    /// Skips the current test when no PostgreSQL server is configured.
    /// </summary>
    public void RequireServer()
        => Assert.SkipWhen(_connectionString is null, "No PostgreSQL server: set ODC_TEST_DATABASE or the user secrets of ODC.Web.");

    /// <summary>
    /// Creates a client that keeps cookies like a browser and has a remote address of its own.
    /// </summary>
    /// <returns>The client.</returns>
    public ApiClient CreateApiClient()
    {
        RequireServer();
        var http = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var number = Interlocked.Increment(ref _lastAddress);
        http.DefaultRequestHeaders.Add(ClientAddressStartupFilter.HeaderName, $"10.{(number >> 16) & 255}.{(number >> 8) & 255}.{number & 255}");
        return new ApiClient(http);
    }

    /// <summary>
    /// Runs code against the application's services in a scope of its own (to look into the database).
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="action">The code.</param>
    /// <returns>Its result.</returns>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(_folder, "spa"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "spa", "index.html"), "<!doctype html><title>ODC</title>", TestContext.Current.CancellationToken);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        await File.WriteAllTextAsync(Path.Combine(_folder, "jwt.pem"), key.ExportPkcs8PrivateKeyPem(), TestContext.Current.CancellationToken);

        // Starting the server runs the migrations and the seeding, as the real start does.
        _ = Server;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_connectionString is null)
        {
            return;
        }

        // Pooled connections keep the databases open: close them and force the drop (PostgreSQL 13+). The organizations'
        // database is named after the master one (OrganizationDatabase).
        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var name = builder.Database;
        builder.Database = "postgres";
        await using (var connection = new NpgsqlConnection(builder.ConnectionString))
        {
            await connection.OpenAsync(CancellationToken.None);
            foreach (var database in new[] { name, name + OrganizationDatabase.NameSuffix })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }

        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // A log file still held open: the system cleans its temporary directory.
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Development: Data Protection keys may stay unencrypted off Windows (CI); the settings below replace what
        // appsettings.Development.json and the user secrets would bring.
        builder.UseEnvironment(Environments.Development);
        foreach (var (key, value) in Settings())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IStartupFilter, ClientAddressStartupFilter>();
        });
    }

    /// <summary>
    /// The configuration of the application under test.
    /// </summary>
    /// <returns>The settings.</returns>
    private Dictionary<string, string?> Settings() => new(StringComparer.Ordinal)
    {
        ["ConnectionStrings:Database"] = _connectionString,
        ["Seed:AdminEmail"] = AdminEmail,
        ["Seed:DemoData"] = "true",
        ["Seed:DemoPassword"] = DemoPassword,
        ["Email:Host"] = string.Empty,
        ["Email:FromAddress"] = "odc@odc.local",
        ["Email:PickupDirectory"] = Mailbox.Folder,
        ["Email:SiteUrl"] = "https://localhost",
        ["Authentication:Jwt:SigningKeyPath"] = Path.Combine(_folder, "jwt.pem"),
        ["Authentication:Passwords:CheckBreachedPasswords"] = "false",
        ["Authentication:Activation:MinimumRequestDuration"] = "00:00:00",
        ["DataProtection:CertificatePath"] = string.Empty,
        ["Storage:PrivateRoot"] = Path.Combine(_folder, "storage"),
        ["Signing:TrustList:RefreshInterval"] = "00:00:00",
        ["Signing:TrustList:CacheFolder"] = Path.Combine(_folder, "eusign"),
        ["Logs:FilePath"] = Path.Combine(_folder, "logs", "odc-.log"),
        ["Spa:Root"] = Path.Combine(_folder, "spa"),
        ["Serilog:MinimumLevel:Default"] = "Warning",
    };
}
