using ODC.Domain.Chats;

namespace ODC.Application.Features.Chats;

/// <summary>
/// A file or a photo of a chat message.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Kind">The kind.</param>
/// <param name="FileName">The file name.</param>
/// <param name="ContentType">The media type.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="Width">The width of a photo.</param>
/// <param name="Height">The height of a photo.</param>
/// <param name="Url">The address of the file (members only).</param>
/// <param name="ThumbnailUrl">The address of the thumbnail of a photo.</param>
public sealed record ChatAttachmentDto(
    Guid Id,
    ChatAttachmentKind Kind,
    string FileName,
    string ContentType,
    long Size,
    int? Width,
    int? Height,
    string Url,
    string? ThumbnailUrl);
