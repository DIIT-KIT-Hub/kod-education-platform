using System.Globalization;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Shared.Media;

namespace ODC.Web.Infrastructure.Media;

/// <summary>
/// Stores image renditions under <c>{PrivateRoot}/platform/media/{last two hex digits}/{id}</c> (and <c>{id}.thumb</c>),
/// encrypted with the platform's key (<see cref="EncryptedFile"/>) and outside <c>wwwroot</c>: they are served only to
/// signed-in users. Paths are built from the identifier alone; the last digits are random in a UUID v7 (the first ones
/// are a timestamp), so the 256 folders fill evenly.
/// </summary>
internal sealed class LocalMediaStorage : IMediaStorage
{
    /// <summary>
    /// The absolute media folder.
    /// </summary>
    private readonly string _root;

    /// <summary>
    /// The keys of the files.
    /// </summary>
    private readonly FileKeys _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalMediaStorage"/> class.
    /// </summary>
    /// <param name="options">The storage options.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="keys">The keys of the files.</param>
    public LocalMediaStorage(IOptions<StorageOptions> options, IHostEnvironment environment, FileKeys keys)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _root = Path.Combine(StorageOptions.PlatformFolder(StorageOptions.RootOf(options.Value, environment)), "media");
        _keys = keys;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Guid id, ProcessedImage image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);

        var key = await _keys.PlatformAsync(cancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(PathOf(id, thumbnail: false))!);
        await WriteAsync(PathOf(id, thumbnail: false), image.Main, key, cancellationToken);
        await WriteAsync(PathOf(id, thumbnail: true), image.Thumbnail, key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenReadAsync(Guid id, bool thumbnail, CancellationToken cancellationToken)
    {
        var key = await _keys.PlatformAsync(cancellationToken);
        try
        {
            var file = new FileStream(PathOf(id, thumbnail), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 16384, FileOptions.Asynchronous | FileOptions.RandomAccess);
            return EncryptedFile.Read(file, key);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Delete(Guid id)
    {
        // File.Delete ignores a missing file but not a missing folder (an image whose files were never written).
        var main = PathOf(id, thumbnail: false);
        if (Directory.Exists(Path.GetDirectoryName(main)))
        {
            File.Delete(main);
            File.Delete(PathOf(id, thumbnail: true));
        }
    }

    /// <summary>
    /// Writes a file through a temporary name, so a reader never sees a half-written image.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="content">The content.</param>
    /// <param name="key">The platform's key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when written.</returns>
    private static async Task WriteAsync(string path, byte[] content, byte[] key, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        await using (var target = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 16384, FileOptions.Asynchronous))
        {
            using var writer = EncryptedFile.Write(target, key);
            await writer.WriteAsync(content, cancellationToken);
            await writer.CompleteAsync(cancellationToken);
        }

        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>
    /// Returns the path of a rendition.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="thumbnail">Whether the thumbnail is meant.</param>
    /// <returns>The absolute path.</returns>
    private string PathOf(Guid id, bool thumbnail)
    {
        var name = id.ToString("N", CultureInfo.InvariantCulture);
        return Path.Combine(_root, name[^2..], thumbnail ? $"{name}.thumb" : name);
    }
}
