using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;

namespace ODC.Web.Shared.Files;

/// <summary>
/// Checks and stores the files of a chat or request message in the private storage: photos are optimized to AVIF with a
/// thumbnail (metadata stripped), other files of the allow-list (<see cref="FileRules.ChatFileTypes"/>) are stored as
/// they are and always downloaded.
/// </summary>
/// <param name="images">Optimizes photos.</param>
/// <param name="files">The private file storage.</param>
public sealed class AttachmentStore(IImageProcessor images, IFileStorage files)
{
    /// <summary>
    /// Checks and stores the files of a message.
    /// </summary>
    /// <param name="uploads">The uploaded files.</param>
    /// <param name="category">The storage folder.</param>
    /// <param name="maxCount">The largest number of files of one message.</param>
    /// <param name="notAllowed">The error text of a file type that is not allowed (the extension is its value).</param>
    /// <param name="stored">Collects the keys of the stored files (deleted again when the message is not saved).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored files.</returns>
    public async Task<Result<IReadOnlyList<StoredAttachment>>> StoreAsync(
        IReadOnlyList<FileUpload> uploads,
        string category,
        int maxCount,
        Func<string, string> notAllowed,
        List<string> stored,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uploads);
        ArgumentNullException.ThrowIfNull(notAllowed);
        ArgumentNullException.ThrowIfNull(stored);
        if (uploads.Count > maxCount)
        {
            return Error.Validation(Texts.T("До повідомлення можна додати не більше {0} файлів.", maxCount));
        }

        if (uploads.Sum(static f => f.Length) > FileRules.MaxChatMessageSize)
        {
            return Error.Validation(Texts.T("Файли одного повідомлення не можуть бути більшими за {0} МБ разом.", FileRules.MaxChatMessageSize / 1024 / 1024));
        }

        var attachments = new List<StoredAttachment>();
        foreach (var upload in uploads)
        {
            var type = FileRules.ResolveChatFileType(upload.FileName);
            if (type is null)
            {
                return Error.Validation(notAllowed(Path.GetExtension(upload.FileName)));
            }

            if (upload.Length > FileRules.MaxChatFileSize)
            {
                return Error.Validation(Texts.T("Розмір файлу перевищує {0} МБ.", FileRules.MaxChatFileSize / 1024 / 1024));
            }

            if (FileRules.ChatPhotoExtensions.Contains(Path.GetExtension(upload.FileName)) && upload.Length <= FileRules.MaxImageSize)
            {
                using var buffer = new MemoryStream();
                await upload.Content.CopyToAsync(buffer, cancellationToken);
                var photo = await images.ProcessAsync(buffer.ToArray(), MediaPurpose.Chat, crop: null, cancellationToken);
                if (photo is not null)
                {
                    var main = await files.SaveAsync(new MemoryStream(photo.Main), category, FileRules.MaxChatFileSize, cancellationToken);
                    stored.Add(main.StorageKey);
                    var small = await files.SaveAsync(new MemoryStream(photo.Thumbnail), category, FileRules.MaxChatFileSize, cancellationToken);
                    stored.Add(small.StorageKey);
                    attachments.Add(new StoredAttachment(upload.FileName, "image/avif", main.Size, main.StorageKey, small.StorageKey, photo.Width, photo.Height));
                    continue;
                }

                // Not a readable photo after all: it is kept as the file it is.
                buffer.Position = 0;
                var original = await files.SaveAsync(buffer, category, FileRules.MaxChatFileSize, cancellationToken);
                stored.Add(original.StorageKey);
                attachments.Add(new StoredAttachment(upload.FileName, type, original.Size, original.StorageKey, null, null, null));
                continue;
            }

            var blob = await files.SaveAsync(upload.Content, category, FileRules.MaxChatFileSize, cancellationToken);
            stored.Add(blob.StorageKey);
            attachments.Add(new StoredAttachment(upload.FileName, type, blob.Size, blob.StorageKey, null, null, null));
        }

        return Result<IReadOnlyList<StoredAttachment>>.Success(attachments);
    }

    /// <summary>
    /// Deletes stored files; a file that cannot be deleted stays (it is unreachable without its row).
    /// </summary>
    /// <param name="keys">The storage keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    public async Task DeleteAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
        {
            try
            {
                await files.DeleteAsync(key, CancellationToken.None);
            }
            catch (IOException)
            {
                // Locked by a download in progress; harmless.
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
