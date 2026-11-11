using ODC.Web.Infrastructure.Persistence.Seeding;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Hosting;

/// <summary>
/// The operator's commands: the application started with a command does that work with its usual settings and stops,
/// without serving requests.
/// <list type="bullet">
/// <item><c>migrate</c>: migrates the master and every organization's database and seeds (what every start does).</item>
/// <item><c>backup CODE|--all</c>: an encrypted backup of an organization (its database and files) into
/// <c>Organizations:BackupRoot</c>.</item>
/// <item><c>restore CODE FOLDER</c>: replaces an organization's database and files with its backup.</item>
/// <item><c>rotate-passwords CODE|--all</c>: new random passwords of an organization's database roles.</item>
/// </list>
/// </summary>
internal static class OperatorCommands
{
    /// <summary>
    /// The commands.
    /// </summary>
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase) { "migrate", "backup", "restore", "rotate-passwords" };

    /// <summary>
    /// Splits the command (and its own arguments) from the host's settings (<c>--key=value</c>).
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <returns>The command with its arguments (null: serve requests) and the host's arguments.</returns>
    public static (string[]? Command, string[] HostArguments) Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 0 || !Names.Contains(args[0]))
        {
            return (null, args);
        }

        var command = args.TakeWhile(static a => !a.StartsWith("--", StringComparison.Ordinal) || a == "--all").ToArray();
        return (command, args[command.Length..]);
    }

    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <param name="app">The application (built, not started).</param>
    /// <param name="command">The command and its arguments.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(WebApplication app, string[] command)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(command);
        var cancellationToken = app.Lifetime.ApplicationStopping;
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        switch (command[0].ToLowerInvariant())
        {
            case "migrate":
                await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
                Console.WriteLine("The databases are up to date.");
                return 0;

            case "backup" when command.Length == 2:
                foreach (var organization in await ChooseAsync(services, command[1], cancellationToken))
                {
                    var folder = await services.GetRequiredService<OrganizationBackups>().BackupAsync(organization, cancellationToken);
                    Console.WriteLine($"{organization.Code}: {folder}");
                }

                return 0;

            case "restore" when command.Length == 3:
                {
                    var organization = (await ChooseAsync(services, command[1], cancellationToken)).Single();
                    var manifest = await services.GetRequiredService<OrganizationBackups>().RestoreAsync(organization, command[2], cancellationToken);
                    Console.WriteLine($"{organization.Code}: restored the backup of {manifest.CreatedAt:u}. Restart the running application (it keeps the old database's type identifiers); an older backup was migrated to the current schema.");
                    return 0;
                }

            case "rotate-passwords" when command.Length == 2:
                foreach (var organization in await ChooseAsync(services, command[1], cancellationToken))
                {
                    var rotated = await services.GetRequiredService<OrganizationProvisioning>().RotatePasswordsAsync(organization.Id, cancellationToken);
                    Console.WriteLine($"{organization.Code}: {(rotated.IsSuccess ? "new passwords" : rotated.Error.Message)}");
                }

                return 0;

            default:
                Console.Error.WriteLine("Usage: migrate | backup CODE|--all | restore CODE FOLDER | rotate-passwords CODE|--all");
                return 2;
        }
    }

    /// <summary>
    /// The organizations a command works on (every one that is not being created or deleted, or the one of the code).
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="choice">A code or <c>--all</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organizations.</returns>
    private static async Task<List<Organization>> ChooseAsync(IServiceProvider services, string choice, CancellationToken cancellationToken)
    {
        var registry = services.GetRequiredService<OrganizationRegistry>();
        var all = (await registry.AllAsync(cancellationToken)).Where(static o => o.State is OrganizationState.Active or OrganizationState.Suspended).ToList();
        if (choice == "--all")
        {
            return all;
        }

        var chosen = all.FirstOrDefault(o => string.Equals(o.Code, choice, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No organization with the code {choice}.");
        return [chosen];
    }
}
