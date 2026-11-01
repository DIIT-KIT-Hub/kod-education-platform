namespace KOD.Application.Features.Tickets;

/// <summary>
/// A file of a request message.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="FileName">The file name.</param>
/// <param name="ContentType">The media type.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="IsImage">Whether it is a photo shown in the history (with a thumbnail).</param>
/// <param name="Width">The width of a photo.</param>
/// <param name="Height">The height of a photo.</param>
/// <param name="Url">The download address.</param>
/// <param name="ThumbnailUrl">The address of the thumbnail of a photo.</param>
public sealed record TicketAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    bool IsImage,
    int? Width,
    int? Height,
    string Url,
    string? ThumbnailUrl);
