namespace ODC.Application.Common.Files;

/// <summary>
/// A stored file of a message.
/// </summary>
/// <param name="FileName">The file name given by the sender.</param>
/// <param name="ContentType">The media type of the stored file.</param>
/// <param name="Size">The size of the stored file in bytes.</param>
/// <param name="StorageKey">The key of the stored file.</param>
/// <param name="ThumbnailKey">The key of the thumbnail of a photo.</param>
/// <param name="Width">The width of a photo.</param>
/// <param name="Height">The height of a photo.</param>
public sealed record StoredAttachment(string FileName, string ContentType, long Size, string StorageKey, string? ThumbnailKey, int? Width, int? Height);
