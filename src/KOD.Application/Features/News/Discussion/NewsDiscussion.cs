namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// The reactions and comments under an article.
/// </summary>
/// <param name="Reactions">The reactions to the article.</param>
/// <param name="Comments">The newest top-level comments with their replies.</param>
/// <param name="CommentCount">The number of all comments and replies (deleted ones excluded).</param>
/// <param name="HasMore">Whether older top-level comments exist.</param>
/// <param name="CanReact">Whether the current user may react and comment at all (the article is published).</param>
/// <param name="CanComment">Whether comments are open.</param>
public sealed record NewsDiscussion(
    ReactionSummary Reactions,
    IReadOnlyList<NewsCommentDto> Comments,
    int CommentCount,
    bool HasMore,
    bool CanReact,
    bool CanComment);
