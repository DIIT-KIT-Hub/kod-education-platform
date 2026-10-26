namespace KOD.Application.Abstractions;

/// <summary>
/// Stores the renditions of uploaded images (<see cref="Domain.Media.MediaFile"/>). Files are immutable and addressed by the image identifier.
/// </summary>
public interface IMediaStorage
{
    /// <summary>
    /// Saves both renditions.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="image">The renditions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task SaveAsync(Guid id, ProcessedImage image, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a rendition for reading.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="thumbnail">Whether to open the thumbnail rather than the main rendition.</param>
    /// <returns>The stream, or <see langword="null"/> when the image does not exist.</returns>
    Stream? OpenRead(Guid id, bool thumbnail);

    /// <summary>
    /// Deletes both renditions (missing files are ignored).
    /// </summary>
    /// <param name="id">The image identifier.</param>
    void Delete(Guid id);
}
