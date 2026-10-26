using KOD.Application.Common.Models;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// A comment with its replies.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="ParentId">The comment this one replies to.</param>
/// <param name="Author">The author.</param>
/// <param name="Body">The plain text (empty when deleted).</param>
/// <param name="CreatedAt">The moment of writing.</param>
/// <param name="EditedAt">The moment of the last edit.</param>
/// <param name="IsDeleted">Whether the comment was deleted (kept as a placeholder for its replies).</param>
/// <param name="Reactions">The reactions.</param>
/// <param name="CanEdit">Whether the current user may edit the comment (its author).</param>
/// <param name="CanDelete">Whether the current user may delete the comment (its author or a moderator).</param>
/// <param name="Replies">The replies, oldest first.</param>
public sealed record NewsCommentDto(
    Guid Id,
    Guid? ParentId,
    UserRef Author,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    bool IsDeleted,
    ReactionSummary Reactions,
    bool CanEdit,
    bool CanDelete,
    IReadOnlyList<NewsCommentDto> Replies);
