using System.Text.RegularExpressions;
using KOD.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Files;

/// <summary>
/// Deletes the public images of earlier versions from <c>wwwroot/{PublicUploadsFolder}</c>.
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
    /// Matches the folder/file part of upload URLs.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex(@"^[a-z]{1,32}/[0-9a-f]{32}\.(png|jpg|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex UrlTailPattern();
}
