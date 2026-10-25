using Microsoft.Extensions.Options;

namespace KOD.Web.Signing;

/// <summary>
/// Holds the current trust list. On first use it loads the last downloaded copy (<see cref="TrustListOptions.CacheFolder"/>)
/// or, when there is none yet, the copy bundled with the application (<c>wwwroot/lib/eusign/data</c>);
/// <see cref="TrustListRefreshService"/> replaces it with fresh files from the official source.
/// </summary>
/// <param name="environment">The host environment.</param>
/// <param name="options">The signing options.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class TrustListStore(
    IWebHostEnvironment environment,
    IOptions<SigningOptions> options,
    ILogger<TrustListStore> logger)
{
    /// <summary>
    /// The provider list file name.
    /// </summary>
    private const string ProvidersFile = "CAs.json";

    /// <summary>
    /// The certificate bundle file name.
    /// </summary>
    private const string CertificatesFile = "CACertificates.p7b";

    /// <summary>
    /// Serializes the first load and the replacements.
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>
    /// The current snapshot.
    /// </summary>
    private TrustList? _current;

    /// <summary>
    /// Gets the current trust list.
    /// </summary>
    public TrustList Current => Volatile.Read(ref _current) ?? LoadInitial();

    /// <summary>
    /// Gets the folder with the last downloaded copy.
    /// </summary>
    private string CacheFolder => Path.GetFullPath(options.Value.TrustList.CacheFolder, environment.ContentRootPath);

    /// <summary>
    /// Stores a newer trust list on disk (atomically) and makes it current.
    /// </summary>
    /// <param name="list">The validated trust list.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the list is saved.</returns>
    public async Task ReplaceAsync(TrustList list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);

        var folder = CacheFolder;
        Directory.CreateDirectory(folder);
        await WriteAtomicallyAsync(Path.Combine(folder, CertificatesFile), list.Certificates, cancellationToken);
        await WriteAtomicallyAsync(Path.Combine(folder, ProvidersFile), list.Providers, cancellationToken);

        lock (_gate)
        {
            Volatile.Write(ref _current, list);
        }
    }

    /// <summary>
    /// Writes a file through a temporary file so readers never see a partial file.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the file is replaced.</returns>
    private static async Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, content, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>
    /// Logs the loaded list.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="providers">The number of providers.</param>
    /// <param name="certificates">The number of certificates.</param>
    /// <param name="updatedAt">When the list was obtained.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Trust list loaded: {Providers} providers, {Certificates} CA certificates, obtained {UpdatedAt}")]
    private static partial void LogLoaded(ILogger logger, int providers, int certificates, DateTimeOffset updatedAt);

    /// <summary>
    /// Logs a rejected copy.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="folder">The folder.</param>
    /// <param name="reason">The reason.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring the trust list in {Folder}: {Reason}")]
    private static partial void LogInvalidCopy(ILogger logger, string folder, string reason);

    /// <summary>
    /// Tries to load a trust list from a folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="list">The trust list.</param>
    /// <returns><see langword="true"/> when both files exist and are valid.</returns>
    private bool TryLoad(string folder, out TrustList? list)
    {
        list = null;
        var providersPath = Path.Combine(folder, ProvidersFile);
        var certificatesPath = Path.Combine(folder, CertificatesFile);
        if (!File.Exists(providersPath) || !File.Exists(certificatesPath))
        {
            return false;
        }

        var updatedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(providersPath), TimeSpan.Zero);
        if (TrustList.TryCreate(File.ReadAllBytes(providersPath), File.ReadAllBytes(certificatesPath), updatedAt, out list, out var error))
        {
            return true;
        }

        LogInvalidCopy(logger, folder, error);
        return false;
    }

    /// <summary>
    /// Loads the last downloaded copy or the bundled one.
    /// </summary>
    /// <returns>The trust list.</returns>
    private TrustList LoadInitial()
    {
        lock (_gate)
        {
            if (_current is { } loaded)
            {
                return loaded;
            }

            if (!TryLoad(CacheFolder, out var list) && !TryLoad(Path.Combine(environment.WebRootPath, "lib", "eusign", "data"), out list))
            {
                throw new InvalidOperationException("No valid trust list (CAs.json, CACertificates.p7b) is available.");
            }

            LogLoaded(logger, list!.ProviderCount, list.CertificateCount, list.UpdatedAt);
            Volatile.Write(ref _current, list);
            return list;
        }
    }
}
