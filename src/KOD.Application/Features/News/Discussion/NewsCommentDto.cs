using KOD.Application.Common.Models;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// A comment with the branch of replies under it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="ParentId">The comment this one replies to.</param>
/// <param name="ReplyTo">The author of the comment this one replies to (shown as "in reply to …").</param>
/// <param name="Depth">The nesting level: 0 for a top-level comment.</param>
/// <param name="Author">The author.</param>
/// <param name="Body">The plain text (empty when deleted).</param>
/// <param name="CreatedAt">The moment of writing.</param>
/// <param name="EditedAt">The moment of the last edit.</param>
/// <param name="IsDeleted">Whether the comment was deleted (kept as a placeholder for its replies).</param>
/// <param name="Reactions">The reactions.</param>
/// <param name="CanEdit">Whether the current user may edit the comment (its author).</param>
/// <param name="CanDelete">Whether the current user may delete the comment (its author or a moderator).</param>
/// <param name="ReplyCount">The number of replies in the whole branch under this comment (deleted ones excluded).</param>
/// <param name="Replies">The direct replies, oldest first, each with its own branch.</param>
public sealed record NewsCommentDto(
    Guid Id,
    Guid? ParentId,
    UserRef? ReplyTo,
    int Depth,
    UserRef Author,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    bool IsDeleted,
    ReactionSummary Reactions,
    bool CanEdit,
    bool CanDelete,
    int ReplyCount,
    IReadOnlyList<NewsCommentDto> Replies);
