using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Backs up and restores one organization: its database (<c>pg_dump</c> in the custom format, encrypted with the
/// organization's key: <c>database.dump.odc</c>) and its files (already encrypted: <c>files.zip</c>), with a manifest, in
/// a folder of its own. Restoring one organization touches no other. The key stays in the registry of the master
/// database, so a backup opens only with the master database (backed up separately) and its Data Protection keys.
/// </summary>
/// <param name="databases">Where the databases are.</param>
/// <param name="admin">Creates and drops databases.</param>
/// <param name="keys">The keys of the files.</param>
/// <param name="options">The settings (tools and the backup folder).</param>
/// <param name="storage">The storage settings.</param>
/// <param name="environment">The host environment.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class OrganizationBackups(
    OrganizationDatabases databases,
    OrganizationDatabaseAdmin admin,
    FileKeys keys,
    IOptions<OrganizationOptions> options,
    IOptions<StorageOptions> storage,
    IHostEnvironment environment,
    TimeProvider timeProvider)
{
    /// <summary>
    /// The encrypted dump in a backup folder.
    /// </summary>
    public const string DumpFile = "database.dump.odc";

    /// <summary>
    /// The files in a backup folder.
    /// </summary>
    public const string FilesFile = "files.zip";

    /// <summary>
    /// The manifest in a backup folder.
    /// </summary>
    public const string ManifestFile = "manifest.json";

    /// <summary>
    /// The JSON settings of the manifest.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Backs up an organization into a new folder.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The folder of the backup.</returns>
    public async Task<string> BackupAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var now = timeProvider.GetUtcNow();
        var folder = Path.Combine(
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.BackupRoot)),
            string.Create(CultureInfo.InvariantCulture, $"{organization.Code.ToLowerInvariant()}-{now:yyyyMMdd-HHmmss}"));
        Directory.CreateDirectory(folder);
        try
        {
            var key = keys.Of(organization);
            await using (var target = new FileStream(Path.Combine(folder, DumpFile), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                using var dump = Start("pg_dump", organization, ["--format=custom", "--no-owner", "--no-privileges"], redirectInput: false);
                var errors = dump.StandardError.ReadToEndAsync(cancellationToken);
                await EncryptedFile.EncryptAsync(dump.StandardOutput.BaseStream, target, key, cancellationToken);
                await dump.WaitForExitAsync(cancellationToken);
                if (dump.ExitCode != 0)
                {
                    throw new InvalidOperationException($"pg_dump of {organization.Code} failed: {(await errors).Trim()}");
                }
            }

            var files = StorageOptions.OrganizationFolder(StorageOptions.RootOf(storage.Value, environment), organization.Id);
            await using (var zip = new FileStream(Path.Combine(folder, FilesFile), FileMode.CreateNew))
            {
                using var archive = new ZipArchive(zip, ZipArchiveMode.Create);
                if (Directory.Exists(files))
                {
                    foreach (var file in Directory.EnumerateFiles(files, "*", SearchOption.AllDirectories))
                    {
                        // The files are encrypted already: stored, not compressed.
                        await archive.CreateEntryFromFileAsync(file, Path.GetRelativePath(files, file).Replace('\\', '/'), CompressionLevel.NoCompression, cancellationToken);
                    }
                }
            }

            var manifest = new BackupManifest(1, organization.Id, organization.Code, organization.Name, organization.DatabaseName, organization.SchemaVersion, now);
            await File.WriteAllTextAsync(Path.Combine(folder, ManifestFile), JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);
            return folder;
        }
        catch
        {
            Directory.Delete(folder, recursive: true);
            throw;
        }
    }

    /// <summary>
    /// Replaces an organization's database and files with a backup of the same organization.
    /// </summary>
    /// <param name="organization">The organization (its key must be the one the backup was made with).</param>
    /// <param name="folder">The backup folder.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The backup's manifest.</returns>
    public async Task<BackupManifest> RestoreAsync(Organization organization, string folder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(await File.ReadAllTextAsync(Path.Combine(folder, ManifestFile), cancellationToken), JsonOptions)
            ?? throw new InvalidOperationException("The backup has no manifest.");
        if (manifest.OrganizationId != organization.Id)
        {
            throw new InvalidOperationException($"The backup is of the organization {manifest.Code}, not of {organization.Code}.");
        }

        var key = keys.Of(organization);

        // Read the first chunk before anything is dropped: a wrong key or a damaged backup stops here.
        await using (var check = EncryptedFile.Read(File.OpenRead(Path.Combine(folder, DumpFile)), key))
        {
            var probe = new byte[1];
            _ = await check.ReadAsync(probe, cancellationToken);
        }

        await admin.DropDatabaseAsync(organization, cancellationToken);
        await admin.CreateAsync(organization, cancellationToken);
        await using (var source = EncryptedFile.Read(File.OpenRead(Path.Combine(folder, DumpFile)), key))
        {
            using var restore = Start("pg_restore", organization, ["--no-owner", "--no-privileges", "--exit-on-error", "--dbname=" + organization.DatabaseName], redirectInput: true);
            var errors = restore.StandardError.ReadToEndAsync(cancellationToken);
            await source.CopyToAsync(restore.StandardInput.BaseStream, cancellationToken);
            restore.StandardInput.Close();
            await restore.WaitForExitAsync(cancellationToken);
            if (restore.ExitCode != 0)
            {
                throw new InvalidOperationException($"pg_restore of {organization.Code} failed: {(await errors).Trim()}");
            }
        }

        var files = StorageOptions.OrganizationFolder(StorageOptions.RootOf(storage.Value, environment), organization.Id);
        if (Directory.Exists(files))
        {
            Directory.Delete(files, recursive: true);
        }

        Directory.CreateDirectory(files);
        await ZipFile.ExtractToDirectoryAsync(Path.Combine(folder, FilesFile), files, cancellationToken);

        // A backup of an older schema is brought up to date at once (the registry already names the latest one).
        await admin.MigrateAsync(organization, cancellationToken);

        // The restore created the extension types (citext) anew: this process forgets their old identifiers. Another
        // running process keeps them until it restarts.
        await using (var connection = new NpgsqlConnection(databases.ConnectionStringOf(organization)))
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ReloadTypesAsync(cancellationToken);
        }

        return manifest;
    }

    /// <summary>
    /// Starts a PostgreSQL tool connected to the organization's database as its owner (the password goes through the
    /// environment, never the command line).
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="organization">The organization.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="redirectInput">Whether the tool reads the standard input.</param>
    /// <returns>The process.</returns>
    private Process Start(string tool, Organization organization, IEnumerable<string> arguments, bool redirectInput)
    {
        var connection = new NpgsqlConnectionStringBuilder(databases.OwnerConnectionStringOf(organization));
        var executable = string.IsNullOrWhiteSpace(options.Value.PostgresTools) ? tool : Path.Combine(options.Value.PostgresTools, OperatingSystem.IsWindows() ? tool + ".exe" : tool);
        var start = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = !redirectInput,
            RedirectStandardError = true,
            RedirectStandardInput = redirectInput,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["PGHOST"] = connection.Host;
        start.Environment["PGPORT"] = connection.Port.ToString(CultureInfo.InvariantCulture);
        start.Environment["PGUSER"] = connection.Username;
        start.Environment["PGPASSWORD"] = connection.Password;
        start.Environment["PGDATABASE"] = connection.Database;
        start.Environment["PGSSLMODE"] = connection.SslMode switch
        {
            SslMode.Disable => "disable",
            SslMode.Allow => "allow",
            SslMode.Require => "require",
            SslMode.VerifyCA => "verify-ca",
            SslMode.VerifyFull => "verify-full",
            _ => "prefer",
        };
        return Process.Start(start) ?? throw new InvalidOperationException($"{tool} did not start.");
    }
}
