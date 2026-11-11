using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Documents;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Admin.Organizations;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.IntegrationTests.Tenancy;

/// <summary>
/// The life of an organization and what keeps its data apart: PostgreSQL roles of its own (nobody else may connect, the
/// application role has no DDL), creation, suspension, resumption and deletion by a platform administrator, files
/// encrypted in its own folder, and a backup that restores one organization without touching another.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class OrganizationLifecycleTests(OdcApplication app)
{
    [Fact]
    public async Task Every_organization_database_has_roles_of_its_own_and_nobody_else_may_connect()
    {
        app.RequireServer();
        var registry = app.Services.GetRequiredService<OrganizationRegistry>();
        var databases = app.Services.GetRequiredService<OrganizationDatabases>();
        var org = (await registry.FindByCodeAsync("ORG", TestContext.Current.CancellationToken))!;
        var fund = (await registry.FindByCodeAsync("FUND", TestContext.Current.CancellationToken))!;

        await using (var connection = new NpgsqlConnection(app.AdministrationConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(OrganizationDatabases.OwnerRole(fund.DatabaseName), await ScalarAsync<string>(connection, "SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = @a", fund.DatabaseName));
            Assert.True(await ScalarAsync<bool>(connection, "SELECT has_database_privilege(@a, @b, 'CONNECT')", OrganizationDatabases.AppRole(fund.DatabaseName), fund.DatabaseName));
            Assert.False(await ScalarAsync<bool>(connection, "SELECT has_database_privilege(@a, @b, 'CONNECT')", OrganizationDatabases.AppRole(org.DatabaseName), fund.DatabaseName));
            Assert.False(await ScalarAsync<bool>(connection, "SELECT has_database_privilege('public', @a, 'CONNECT')", fund.DatabaseName));
            Assert.False(await ScalarAsync<bool>(
                connection,
                "SELECT bool_or(rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls OR rolreplication) FROM pg_roles WHERE rolname IN (@a, @b)",
                OrganizationDatabases.AppRole(fund.DatabaseName),
                OrganizationDatabases.OwnerRole(fund.DatabaseName)));
        }

        // The first organization's role, its password and all, opens nothing of the fund.
        var foreign = new NpgsqlConnectionStringBuilder(databases.ConnectionStringOf(org)) { Database = fund.DatabaseName, Pooling = false };
        await using (var intruder = new NpgsqlConnection(foreign.ConnectionString))
        {
            var refused = await Assert.ThrowsAsync<PostgresException>(() => intruder.OpenAsync(TestContext.Current.CancellationToken));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, refused.SqlState);
        }

        // The application role reads and writes data but changes no schema.
        await using var own = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(databases.ConnectionStringOf(fund)) { Pooling = false }.ConnectionString);
        await own.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await ScalarAsync<long>(own, "SELECT count(*) FROM organization_units") > 0);
        await using var create = new NpgsqlCommand("CREATE TABLE intruder (x int)", own);
        var denied = await Assert.ThrowsAsync<PostgresException>(() => create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }

    [Fact]
    public async Task A_platform_administrator_creates_suspends_resumes_and_deletes_an_organization()
    {
        using var admin = await app.Accounts.AdminAsync();
        var (id, code) = await CreateOrganizationAsync(admin);
        var created = Assert.Single(await admin.GetAsync<List<OrganizationListItem>>("/api/v1/organizations"), o => o.Id == id);
        Assert.Equal(OrganizationState.Active, created.State);
        Assert.True(created.IsCurrent);
        Assert.True(created.OwnRoles);
        using (var open = await admin.GetAsync($"/o/{code}/api/v1/organization-units"))
        {
            Assert.Equal(HttpStatusCode.OK, open.StatusCode);
        }

        // An active organization is suspended before it may be deleted.
        using (var early = await admin.DeleteAsync($"/api/v1/organizations/{id}?confirmation={code}"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);
        }

        await ExpectNoContentAsync(await admin.PostAsync($"/api/v1/organizations/{id}/suspend", null));
        using (var suspended = await admin.GetAsync($"/o/{code}/api/v1/organization-units"))
        {
            Assert.Equal(HttpStatusCode.NotFound, suspended.StatusCode);
        }

        await ExpectNoContentAsync(await admin.PostAsync($"/api/v1/organizations/{id}/resume", null));
        using (var resumed = await admin.GetAsync($"/o/{code}/api/v1/organization-units"))
        {
            Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        }

        await ExpectNoContentAsync(await admin.PostAsync($"/api/v1/organizations/{id}/rotate-passwords", null));
        using (var rotated = await admin.GetAsync($"/o/{code}/api/v1/organization-units"))
        {
            Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        }

        await ExpectNoContentAsync(await admin.PostAsync($"/api/v1/organizations/{id}/suspend", null));
        using (var unconfirmed = await admin.DeleteAsync($"/api/v1/organizations/{id}?confirmation=WRONG"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);
        }

        await ExpectNoContentAsync(await admin.DeleteAsync($"/api/v1/organizations/{id}?confirmation={code}"));
        Assert.DoesNotContain(await admin.GetAsync<List<OrganizationListItem>>("/api/v1/organizations"), o => o.Id == id);
        await using var connection = new NpgsqlConnection(app.AdministrationConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM pg_database WHERE datname = @a", created.DatabaseName));
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM pg_roles WHERE starts_with(rolname, @a)", created.DatabaseName + "_"));
    }

    [Fact]
    public async Task The_files_of_an_organization_are_encrypted_in_its_own_folder()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var director = await app.Accounts.SignInAsync(Demo.Director);
        const string text = "Таємний зміст документа для перевірки шифрування";
        var id = await DocumentTests.CreateAsync(engineer, "Перевірка шифрування", text, director.Context!.User!.Id);
        var org = (await app.Services.GetRequiredService<OrganizationRegistry>().FindByCodeAsync("ORG", TestContext.Current.CancellationToken))!;

        var files = Directory.EnumerateFiles(Path.Combine(app.StorageRoot, "orgs", org.Id.ToString("N")), "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(files);
        var plain = Encoding.UTF8.GetBytes(text);
        foreach (var file in files)
        {
            var bytes = await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken);
            Assert.Equal("ODC1"u8.ToArray(), bytes[..4]);
            Assert.Equal(-1, bytes.AsSpan().IndexOf(plain));
        }

        Assert.All(Directory.EnumerateDirectories(app.StorageRoot), static d => Assert.True(Path.GetFileName(d) is "orgs" or "platform", d));
        using var download = await engineer.GetAsync($"/api/v1/documents/{id}/file");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(text, await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task One_organization_is_restored_from_its_backup_and_another_stays_as_it_was()
    {
        app.RequireServer();
        Assert.SkipWhen(app.PostgresTools is null, "pg_dump and pg_restore were not found: set ODC_TEST_PG_TOOLS.");
        using var admin = await app.Accounts.AdminAsync();
        var (id, code) = await CreateOrganizationAsync(admin);
        var before = $"{code}-B";
        var after = $"{code}-A";
        var fundAfter = $"FUND-R{Random.Shared.Next(100, 999)}";
        await AddUnitAsync(admin, code, id, before);
        Assert.Contains(before, (await admin.GetAsync<JsonElement>($"/o/{code}/api/v1/organization-units")).GetRawText(), StringComparison.Ordinal);

        var registry = app.Services.GetRequiredService<OrganizationRegistry>();
        registry.Invalidate();
        var organization = (await registry.FindAsync(id, TestContext.Current.CancellationToken))!;
        var backups = app.Services.GetRequiredService<OrganizationBackups>();
        var folder = await backups.BackupAsync(organization, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(Path.Combine(folder, OrganizationBackups.DumpFile)));
        Assert.Equal("ODC1"u8.ToArray(), (await File.ReadAllBytesAsync(Path.Combine(folder, OrganizationBackups.DumpFile), TestContext.Current.CancellationToken))[..4]);

        await AddUnitAsync(admin, code, id, after);
        var fundRoot = (await registry.FindByCodeAsync("FUND", TestContext.Current.CancellationToken))!.Id;
        await AddUnitAsync(admin, "FUND", fundRoot, fundAfter);

        var manifest = await backups.RestoreAsync(organization, folder, TestContext.Current.CancellationToken);
        Assert.Equal(id, manifest.OrganizationId);

        var restored = (await admin.GetAsync<JsonElement>($"/o/{code}/api/v1/organization-units")).GetRawText();
        Assert.Contains(before, restored, StringComparison.Ordinal);
        Assert.DoesNotContain(after, restored, StringComparison.Ordinal);
        Assert.Contains(fundAfter, (await admin.GetAsync<JsonElement>("/o/FUND/api/v1/organization-units")).GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a top-level organization through the structure form (a database of its own).
    /// </summary>
    /// <param name="admin">The platform administrator.</param>
    /// <returns>The organization's identifier and code.</returns>
    private static async Task<(Guid Id, string Code)> CreateOrganizationAsync(ApiClient admin)
    {
        var types = await admin.GetAsync<JsonElement>("/api/v1/unit-types");
        var type = (types.ValueKind == JsonValueKind.Array ? types : types.GetProperty("items")).EnumerateArray().First(static t => t.GetProperty("isOrganization").GetBoolean());
        var code = $"T{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        using var created = await admin.PostAsync(
            "/api/v1/organization-units",
            new { code, name = $"Тестова організація {code}", shortName = (string?)null, typeId = type.GetProperty("id").GetGuid(), parentId = (Guid?)null, sortOrder = 90 });
        await ApiClient.ExpectAsync(created, HttpStatusCode.Created);
        var listed = Assert.Single(await admin.GetAsync<List<OrganizationListItem>>("/api/v1/organizations"), o => o.Code == code);
        return (listed.Id, code);
    }

    /// <summary>
    /// Adds a division to an organization.
    /// </summary>
    /// <param name="admin">The platform administrator.</param>
    /// <param name="organization">The organization's code.</param>
    /// <param name="parentId">Its root unit.</param>
    /// <param name="code">The division's code.</param>
    /// <returns>A task.</returns>
    private static async Task AddUnitAsync(ApiClient admin, string organization, Guid parentId, string code)
    {
        var types = await admin.GetAsync<JsonElement>("/api/v1/unit-types");
        var division = (types.ValueKind == JsonValueKind.Array ? types : types.GetProperty("items")).EnumerateArray().First(static t => !t.GetProperty("isOrganization").GetBoolean());
        using var created = await admin.PostAsync(
            $"/o/{organization}/api/v1/organization-units",
            new { code, name = $"Відділ {code}", shortName = (string?)null, typeId = division.GetProperty("id").GetGuid(), parentId, sortOrder = 5, isActive = true });
        await ApiClient.ExpectAsync(created, HttpStatusCode.Created);
    }

    /// <summary>
    /// Checks for 204 and disposes the response.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>A task.</returns>
    private static async Task ExpectNoContentAsync(HttpResponseMessage response)
    {
        using (response)
        {
            await ApiClient.ExpectAsync(response, HttpStatusCode.NoContent);
        }
    }

    /// <summary>
    /// Reads one value with positional parameters <c>@a</c>, <c>@b</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The query.</param>
    /// <param name="values">The parameters.</param>
    /// <returns>The value.</returns>
    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, params string[] values)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        for (var i = 0; i < values.Length; i++)
        {
            command.Parameters.AddWithValue(((char)('a' + i)).ToString(), values[i]);
        }

        return (T)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
