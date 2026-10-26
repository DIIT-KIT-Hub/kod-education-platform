using KOD.Domain.Common;
using KOD.Domain.Localization;

namespace KOD.Domain.Media;

/// <summary>
/// An uploaded image optimized to AVIF (a main rendition and a thumbnail). The files are immutable: replacing an image
/// creates a new one, so its URL can be cached forever.
/// </summary>
public sealed class MediaFile : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaFile"/> class.
    /// </summary>
    /// <param name="id">The identifier (the files are stored under it before the row is saved).</param>
    /// <param name="purpose">What the image is used for.</param>
    /// <param name="ownerId">The user who uploaded the image.</param>
    /// <param name="size">The size of both renditions in bytes.</param>
    /// <param name="width">The width of the main rendition.</param>
    /// <param name="height">The height of the main rendition.</param>
    public MediaFile(Guid id, MediaPurpose purpose, Guid ownerId, long size, int width, int height)
    {
        if (width <= 0 || height <= 0 || size <= 0)
        {
            throw new DomainException(Texts.T("Некоректне зображення."));
        }

        Id = id;
        Purpose = purpose;
        OwnerId = ownerId;
        Size = size;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaFile"/> class for EF Core materialization.
    /// </summary>
    private MediaFile()
    {
    }

    /// <summary>
    /// Gets what the image is used for.
    /// </summary>
    public MediaPurpose Purpose { get; private set; }

    /// <summary>
    /// Gets the user who uploaded the image.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Gets the size of both renditions in bytes.
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// Gets the width of the main rendition in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the main rendition in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the article that uses the image; <see langword="null"/> until the article is saved (unused images are removed later).
    /// </summary>
    public Guid? NewsArticleId { get; private set; }

    /// <summary>
    /// Marks the image as used by an article.
    /// </summary>
    /// <param name="articleId">The article.</param>
    public void AttachTo(Guid articleId) => NewsArticleId = articleId;
}
