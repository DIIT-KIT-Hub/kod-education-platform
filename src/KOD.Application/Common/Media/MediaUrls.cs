using System.Globalization;
using System.Text.RegularExpressions;

namespace KOD.Application.Common.Media;

/// <summary>
/// The URLs of uploaded images (<c>/media/{id}</c> and <c>/media/{id}/thumb</c>, served to signed-in users with
/// immutable caching) and their parsing from stored HTML.
/// </summary>
public static partial class MediaUrls
{
    /// <summary>
    /// Returns the URL of the main rendition.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <returns>The site-relative URL.</returns>
    public static string Of(Guid id) => string.Create(CultureInfo.InvariantCulture, $"/media/{id:N}");

    /// <summary>
    /// Returns the URL of the thumbnail.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <returns>The site-relative URL.</returns>
    public static string ThumbnailOf(Guid id) => string.Create(CultureInfo.InvariantCulture, $"/media/{id:N}/thumb");

    /// <summary>
    /// Returns the thumbnail URL of an optional image (an avatar).
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <returns>The URL or <see langword="null"/>.</returns>
    public static string? ThumbnailOf(Guid? id) => id is { } value ? ThumbnailOf(value) : null;

    /// <summary>
    /// Determines whether a URL points to an uploaded image and returns its identifier.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="id">The image identifier.</param>
    /// <returns><see langword="true"/> for <c>/media/{id}</c> and <c>/media/{id}/thumb</c>.</returns>
    public static bool TryParse(string? url, out Guid id)
    {
        id = Guid.Empty;
        var match = url is null ? null : UrlPattern().Match(url);
        return match is { Success: true } && Guid.TryParseExact(match.Groups["id"].Value, "N", out id);
    }

    /// <summary>
    /// Returns the identifiers of all uploaded images an HTML fragment refers to.
    /// </summary>
    /// <param name="html">The HTML.</param>
    /// <returns>The distinct identifiers.</returns>
    public static IReadOnlySet<Guid> IdsIn(string? html)
    {
        var ids = new HashSet<Guid>();
        foreach (Match match in ReferencePattern().Matches(html ?? string.Empty))
        {
            ids.Add(Guid.ParseExact(match.Groups["id"].Value, "N"));
        }

        return ids;
    }

    /// <summary>
    /// Matches a complete image URL.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("^/media/(?<id>[0-9a-f]{32})(/thumb)?$", RegexOptions.CultureInvariant)]
    private static partial Regex UrlPattern();

    /// <summary>
    /// Matches image URLs inside HTML.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("/media/(?<id>[0-9a-f]{32})", RegexOptions.CultureInvariant)]
    private static partial Regex ReferencePattern();
}
