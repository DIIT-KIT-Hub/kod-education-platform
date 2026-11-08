namespace ODC.Web.Features.Chats.Domain;

/// <summary>
/// The kind of a chat attachment.
/// </summary>
public enum ChatAttachmentKind
{
    /// <summary>A file of an allowed type, stored as uploaded and always downloaded (never opened in the page).</summary>
    File,

    /// <summary>A photo optimized to AVIF with a thumbnail, shown in the conversation.</summary>
    Image,
}
