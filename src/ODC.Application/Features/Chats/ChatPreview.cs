using ODC.Application.Common.Models;
using ODC.Domain.Chats;

namespace ODC.Application.Features.Chats;

/// <summary>
/// The newest message of a chat, as the chat list shows it.
/// </summary>
/// <param name="Author">The author.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Event">What a system note tells.</param>
/// <param name="Text">The visible text (the details of a system note).</param>
/// <param name="IsDeleted">Whether the message was deleted.</param>
/// <param name="AttachmentCount">The number of attachments.</param>
/// <param name="CreatedAt">The moment of writing.</param>
public sealed record ChatPreview(
    UserRef Author,
    ChatMessageKind Kind,
    ChatEvent? Event,
    string Text,
    bool IsDeleted,
    int AttachmentCount,
    DateTimeOffset CreatedAt);
