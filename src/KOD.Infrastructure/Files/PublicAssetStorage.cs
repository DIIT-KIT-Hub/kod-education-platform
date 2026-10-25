using System.Text.RegularExpressions;
using KOD.Application.Abstractions;
using KOD.Application.Common.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Files;

/// <summary>
/// Stores validated public images in <c>wwwroot/{PublicUploadsFolder}</c>.
/// </summary>
internal sealed partial class PublicAssetStorage : IPublicAssetStorage
{
    /// <summary>
    /// The absolute uploads folder.
    /// </summary>
    private readonly string _root;

    /// <summary>
    /// The URL prefix of uploads.
    /// </summary>
    private readonly string _urlPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicAssetStorage"/> class.
    /// </summary>
    /// <param name="options">The storage options.</param>
    /// <param name="environment">The web host environment.</param>
    public PublicAssetStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        var folder = options.Value.PublicUploadsFolder.Trim('/');
        _root = Path.GetFullPath(Path.Combine(environment.WebRootPath, folder));
        _urlPrefix = $"/{folder}/";
    }

    /// <inheritdoc />
    public async Task<string?> SaveImageAsync(Stream content, string folder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!FolderPattern().IsMatch(folder))
        {
            throw new ArgumentException("Invalid folder.", nameof(folder));
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length is 0 or > FileRules.MaxImageSize)
        {
            return null;
        }

        var extension = DetectImage(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (extension is null)
        {
            return null;
        }

        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), buffer.ToArray(), cancellationToken);
        return $"{_urlPrefix}{folder}/{fileName}";
    }

    /// <inheritdoc />
    public void Delete(string url)
    {
        if (url is null || !url.StartsWith(_urlPrefix, StringComparison.Ordinal) || !UrlTailPattern().IsMatch(url[_urlPrefix.Length..]))
        {
            return;
        }

        var path = Path.GetFullPath(Path.Combine(_root, url[_urlPrefix.Length..]));
        if (path.StartsWith(_root, StringComparison.Ordinal) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Detects PNG, JPEG and WebP by their magic numbers.
    /// </summary>
    /// <param name="data">The file content.</param>
    /// <returns>The extension or <see langword="null"/>.</returns>
    private static string? DetectImage(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return ".png";
        }

        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return ".jpg";
        }

        return data.Length > 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8) ? ".webp" : null;
    }

    /// <summary>
    /// Matches upload folders.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("^[a-z]{1,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex FolderPattern();

    /// <summary>
    /// Matches the folder/file part of upload URLs.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex(@"^[a-z]{1,32}/[0-9a-f]{32}\.(png|jpg|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex UrlTailPattern();
}
