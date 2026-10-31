using KOD.Application.Common.Models;
using KOD.Domain.Chats;

namespace KOD.Application.Features.Chats;

/// <summary>
/// A chat message. It is the same for every member (it is pushed to all of them), so it carries no per-reader flags:
/// the client compares the author and the reacting people with the current user.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="ChatId">The chat.</param>
/// <param name="Sequence">The number in the chat.</param>
/// <param name="Author">The author (for a system note, the person who acted).</param>
/// <param name="Kind">The kind.</param>
/// <param name="Event">What a system note tells.</param>
/// <param name="Body">The sanitized HTML (empty for a system note and once deleted).</param>
/// <param name="Text">The visible text (the details of a system note).</param>
/// <param name="CreatedAt">The moment of writing.</param>
/// <param name="EditedAt">The moment of the last edit.</param>
/// <param name="IsDeleted">Whether the message was deleted.</param>
/// <param name="ClientId">The identifier the author's browser gave the message (matches a message being sent).</param>
/// <param name="Attachments">The files and photos.</param>
/// <param name="Reactions">The reactions, by emotion.</param>
public sealed record ChatMessageDto(
    Guid Id,
    Guid ChatId,
    long Sequence,
    UserRef Author,
    ChatMessageKind Kind,
    ChatEvent? Event,
    string Body,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    bool IsDeleted,
    Guid? ClientId,
    IReadOnlyList<ChatAttachmentDto> Attachments,
    IReadOnlyList<ChatReactionDto> Reactions);
