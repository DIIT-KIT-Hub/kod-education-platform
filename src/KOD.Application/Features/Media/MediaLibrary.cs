using KOD.Application.Abstractions;
using KOD.Application.Common.Files;
using KOD.Application.Common.Results;
using KOD.Domain.Localization;
using KOD.Domain.Media;

namespace KOD.Application.Features.Media;

/// <summary>
/// Adds and removes uploaded images for the application services (no access checks of its own). Files and rows stay
/// consistent: files of a removed image are deleted only after the database change is saved, and files of a new image
/// are deleted again when saving fails.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="processor">The image processor.</param>
/// <param name="storage">The image storage.</param>
internal sealed class MediaLibrary(IApplicationDbContext db, IImageProcessor processor, IMediaStorage storage)
{
    /// <summary>
    /// The images stored since the last save.
    /// </summary>
    private readonly List<Guid> _added = [];

    /// <summary>
    /// The images whose files are deleted after the next save.
    /// </summary>
    private readonly List<Guid> _removed = [];

    /// <summary>
    /// Optimizes and stores an uploaded image and tracks its new row (saved by <see cref="SaveChangesAsync"/>).
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="purpose">What the image is used for.</param>
    /// <param name="ownerId">The uploading user.</param>
    /// <param name="crop">The part of the image chosen by the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new image or a validation error.</returns>
    public async Task<Result<MediaFile>> AddAsync(FileUpload file, MediaPurpose purpose, Guid ownerId, ImageCrop? crop, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var data = file.Length <= FileRules.MaxImageSize ? await ReadAsync(file.Content, cancellationToken) : null;
        if (data is null)
        {
            return Error.Validation(Texts.T("Зображення не може бути більшим за {0} МБ.", FileRules.MaxImageSize / 1024 / 1024));
        }

        var image = await processor.ProcessAsync(data, purpose, crop is { IsValid: true } ? crop : null, cancellationToken);
        if (image is null)
        {
            return Error.Validation(Texts.T("Оберіть фото у форматі JPEG, PNG, WebP, AVIF або GIF."));
        }

        var media = new MediaFile(Guid.CreateVersion7(), purpose, ownerId, image.Size, image.Width, image.Height);
        await storage.SaveAsync(media.Id, image, cancellationToken);
        _added.Add(media.Id);
        db.MediaFiles.Add(media);
        return media;
    }

    /// <summary>
    /// Removes an image; its files are deleted after <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <param name="media">The tracked image.</param>
    public void Remove(MediaFile media)
    {
        ArgumentNullException.ThrowIfNull(media);
        db.MediaFiles.Remove(media);
        _removed.Add(media.Id);
    }

    /// <summary>
    /// Saves all pending changes of the unit of work, then deletes the files of removed images.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _added.ForEach(storage.Delete);
            _added.Clear();
            _removed.Clear();
            throw;
        }

        _removed.ForEach(storage.Delete);
        _removed.Clear();
        _added.Clear();
    }

    /// <summary>
    /// Reads an upload into memory, refusing content larger than <see cref="FileRules.MaxImageSize"/>.
    /// </summary>
    /// <param name="content">The upload stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bytes, or <see langword="null"/> when too large.</returns>
    private static async Task<byte[]?> ReadAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > FileRules.MaxImageSize)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.Length == 0 ? null : buffer.ToArray();
    }
}
