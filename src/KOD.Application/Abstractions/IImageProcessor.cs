using KOD.Domain.Media;

namespace KOD.Application.Abstractions;

/// <summary>
/// Converts uploaded photos into small AVIF renditions: the main one fits the size budget of its purpose
/// (10–100 KB for news images) and the thumbnail is used in lists. Metadata (EXIF, GPS) is removed.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Decodes, orients, crops, resizes and encodes an image.
    /// </summary>
    /// <param name="source">The uploaded file.</param>
    /// <param name="purpose">The purpose that decides the sizes (an avatar is square).</param>
    /// <param name="crop">The part of the image chosen by the user, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The renditions, or <see langword="null"/> when the file is not a supported image (JPEG, PNG, WebP, AVIF, GIF).</returns>
    Task<ProcessedImage?> ProcessAsync(ReadOnlyMemory<byte> source, MediaPurpose purpose, ImageCrop? crop, CancellationToken cancellationToken);
}
