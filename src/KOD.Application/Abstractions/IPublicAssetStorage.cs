namespace KOD.Application.Abstractions;

/// <summary>
/// Storage for public assets (news cover images) served from <c>wwwroot/uploads</c>.
/// </summary>
public interface IPublicAssetStorage
{
    /// <summary>
    /// Validates and stores an image (PNG, JPEG or WebP, detected by its signature, not by its name).
    /// </summary>
    /// <param name="content">The image stream.</param>
    /// <param name="folder">The sub-folder (e.g. <c>news</c>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The public site-relative URL, or <see langword="null"/> when the file is not a supported image.</returns>
    Task<string?> SaveImageAsync(Stream content, string folder, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a previously stored asset.
    /// </summary>
    /// <param name="url">The public site-relative URL.</param>
    void Delete(string url);
}
