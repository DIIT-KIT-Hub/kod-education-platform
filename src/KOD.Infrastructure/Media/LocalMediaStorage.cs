using System.Globalization;
using KOD.Application.Abstractions;
using KOD.Infrastructure.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Media;

/// <summary>
/// Stores image renditions under <c>{PrivateRoot}/media/{last two hex digits}/{id}.avif</c> (and <c>{id}.thumb.avif</c>),
/// outside <c>wwwroot</c>: they are served only to signed-in users. Paths are built from the identifier alone; the last
/// digits are random in a UUID v7 (the first ones are a timestamp), so the 256 folders fill evenly.
/// </summary>
internal sealed class LocalMediaStorage : IMediaStorage
{
    /// <summary>
    /// The absolute media folder.
    /// </summary>
    private readonly string _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalMediaStorage"/> class.
    /// </summary>
    /// <param name="options">The storage options.</param>
    /// <param name="environment">The host environment.</param>
    public LocalMediaStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.PrivateRoot, "media"));
        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Guid id, ProcessedImage image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);

        Directory.CreateDirectory(Path.GetDirectoryName(PathOf(id, thumbnail: false))!);
        await WriteAsync(PathOf(id, thumbnail: false), image.Main, cancellationToken);
        await WriteAsync(PathOf(id, thumbnail: true), image.Thumbnail, cancellationToken);
    }

    /// <inheritdoc />
    public Stream? OpenRead(Guid id, bool thumbnail)
    {
        try
        {
            return new FileStream(PathOf(id, thumbnail), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 16384, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Delete(Guid id)
    {
        File.Delete(PathOf(id, thumbnail: false));
        File.Delete(PathOf(id, thumbnail: true));
    }

    /// <summary>
    /// Writes a file through a temporary name, so a reader never sees a half-written image.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when written.</returns>
    private static async Task WriteAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, content, cancellationToken);
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
        return Path.Combine(_root, name[^2..], thumbnail ? $"{name}.thumb.avif" : $"{name}.avif");
    }
}
