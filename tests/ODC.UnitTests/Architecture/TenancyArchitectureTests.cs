namespace ODC.UnitTests.Architecture;

/// <summary>
/// The rules that keep an organization's data in its own database: an organization's database is opened only through the
/// organization of the unit of work (never built by hand), the organization of a unit of work is chosen only by the
/// request pipeline or the server's own scopes, and only the infrastructure knows the databases' credentials.
/// </summary>
public sealed class TenancyArchitectureTests
{
    /// <summary>
    /// The places that may choose the organization of a unit of work.
    /// </summary>
    private static readonly string[] OrganizationChoosers =
    [
        Path.Combine("Infrastructure", "Tenancy", "OrganizationMiddleware.cs"),
        Path.Combine("Infrastructure", "Tenancy", "OrganizationScopes.cs"),
        Path.Combine("Infrastructure", "Tenancy", "OrganizationHubFilter.cs"),
    ];

    [Fact]
    public void An_organization_database_is_never_built_by_hand_outside_its_administration()
    {
        var violations = Sources()
            .Where(static f => f.Relative != Path.Combine("Infrastructure", "Tenancy", "OrganizationDatabaseAdmin.cs"))
            .SelectMany(static f => f.Lines.Where(static l => l.Contains("new OrganizationDbContext(", StringComparison.Ordinal)).Select(l => $"{f.Relative}: {l.Trim()}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Only_the_request_pipeline_and_the_server_scopes_choose_an_organization()
    {
        var violations = Sources()
            .Where(static f => !OrganizationChoosers.Contains(f.Relative))
            .SelectMany(static f => f.Lines.Where(static l => l.Contains("OrganizationContext>().Enter(", StringComparison.Ordinal) || l.Contains("organization.Enter(", StringComparison.Ordinal)).Select(l => $"{f.Relative}: {l.Trim()}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Features_know_neither_connections_nor_the_secrets_of_databases_and_files()
    {
        string[] forbidden = ["NpgsqlConnection", "OrganizationDatabases", "OrganizationDatabaseAdmin", "OrganizationSecrets", "FileKeys", "EncryptedFile"];
        var violations = Sources()
            .Where(static f => f.Relative.StartsWith("Features", StringComparison.Ordinal))
            .SelectMany(f => f.Lines.Where(l => forbidden.Any(word => l.Contains(word, StringComparison.Ordinal))).Select(l => $"{f.Relative}: {l.Trim()}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Organization_files_are_stored_only_through_the_storage()
    {
        // The private folder is reached through LocalFileStorage / LocalMediaStorage (encrypted, per owner); the lifecycle
        // and the backups move whole organization folders.
        string[] allowed =
        [
            Path.Combine("Infrastructure", "Files", "LocalFileStorage.cs"),
            Path.Combine("Infrastructure", "Files", "StorageOptions.cs"),
            Path.Combine("Infrastructure", "Media", "LocalMediaStorage.cs"),
            Path.Combine("Infrastructure", "Tenancy", "OrganizationProvisioning.cs"),
            Path.Combine("Infrastructure", "Tenancy", "OrganizationBackups.cs"),
        ];
        var violations = Sources()
            .Where(f => !allowed.Contains(f.Relative))
            .SelectMany(static f => f.Lines.Where(static l => l.Contains("PrivateRoot", StringComparison.Ordinal) || l.Contains("OrganizationFolder(", StringComparison.Ordinal)).Select(l => $"{f.Relative}: {l.Trim()}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// The C# sources of the server (without the build output and the migrations).
    /// </summary>
    /// <returns>Each file's path relative to the project and its lines.</returns>
    private static IEnumerable<(string Relative, string[] Lines)> Sources()
    {
        var root = Path.Combine(RepositoryRoot(), "src", "ODC.Web");
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(file => (Relative: Path.GetRelativePath(root, file), File: file))
            .Where(static f => !f.Relative.StartsWith("bin", StringComparison.Ordinal) && !f.Relative.StartsWith("obj", StringComparison.Ordinal) && !f.Relative.Contains("Migrations", StringComparison.Ordinal))
            .Select(static f => (f.Relative, File.ReadAllLines(f.File)));
    }

    /// <summary>
    /// Finds the repository root (the folder with the solution file).
    /// </summary>
    /// <returns>The path.</returns>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrganizationalDocumentsCabinet.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
