using KOD.Domain.Common;
using KOD.Domain.Localization;

namespace KOD.Domain.Chats;

/// <summary>
/// A file or a photo of a chat message, kept in the private storage and served only to the members of the chat.
/// </summary>
public sealed class ChatAttachment : Entity
{
    /// <summary>
    /// Maximum length of <see cref="FileName"/>.
    /// </summary>
    public const int FileNameMaxLength = 255;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAttachment"/> class.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="fileName">The file name shown and used for downloads.</param>
    /// <param name="contentType">The media type of the stored file.</param>
    /// <param name="size">The size of the stored file in bytes.</param>
    /// <param name="storageKey">The key of the stored file.</param>
    /// <param name="thumbnailKey">The key of the thumbnail of a photo.</param>
    /// <param name="width">The width of a photo.</param>
    /// <param name="height">The height of a photo.</param>
    public ChatAttachment(ChatAttachmentKind kind, string fileName, string contentType, long size, string storageKey, string? thumbnailKey, int? width, int? height)
    {
        if (size <= 0 || string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException(Texts.T("Файл порожній."));
        }

        Kind = kind;
        FileName = Guard.Text(Path.GetFileName(fileName), FileNameMaxLength, "Назва файлу");
        ContentType = contentType;
        Size = size;
        StorageKey = storageKey;
        ThumbnailKey = thumbnailKey;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAttachment"/> class for EF Core materialization.
    /// </summary>
    private ChatAttachment()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        StorageKey = string.Empty;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Gets the chat (for the access check of a download).
    /// </summary>
    public Guid ChatId { get; private set; }

    /// <summary>
    /// Gets the place among the attachments of the message (0, 1, 2…).
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    public ChatAttachmentKind Kind { get; private set; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the media type of the stored file.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the size of the stored file in bytes.
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// Gets the key of the stored file.
    /// </summary>
    public string StorageKey { get; private set; }

    /// <summary>
    /// Gets the key of the thumbnail of a photo.
    /// </summary>
    public string? ThumbnailKey { get; private set; }

    /// <summary>
    /// Gets the width of a photo in pixels.
    /// </summary>
    public int? Width { get; private set; }

    /// <summary>
    /// Gets the height of a photo in pixels.
    /// </summary>
    public int? Height { get; private set; }

    /// <summary>
    /// Binds the attachment to its message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="position">The place among the attachments of the message.</param>
    internal void AttachTo(ChatMessage message, int position)
    {
        Position = position;
        MessageId = message.Id;
        ChatId = message.ChatId;
    }
}
