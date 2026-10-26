namespace KOD.Application.Features.Media;

/// <summary>
/// An uploaded image.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Url">The URL of the main rendition.</param>
/// <param name="ThumbnailUrl">The URL of the thumbnail.</param>
/// <param name="Width">The width of the main rendition.</param>
/// <param name="Height">The height of the main rendition.</param>
/// <param name="Size">The size of both renditions in bytes.</param>
public sealed record MediaDto(Guid Id, string Url, string ThumbnailUrl, int Width, int Height, long Size);
