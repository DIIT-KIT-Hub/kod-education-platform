namespace KOD.Application.Abstractions;

/// <summary>
/// The AVIF renditions of an uploaded image.
/// </summary>
/// <param name="Main">The main rendition.</param>
/// <param name="Thumbnail">The small rendition for lists and galleries.</param>
/// <param name="Width">The width of the main rendition.</param>
/// <param name="Height">The height of the main rendition.</param>
public sealed record ProcessedImage(byte[] Main, byte[] Thumbnail, int Width, int Height)
{
    /// <summary>
    /// Gets the size of both renditions in bytes.
    /// </summary>
    public long Size => Main.LongLength + Thumbnail.LongLength;
}
