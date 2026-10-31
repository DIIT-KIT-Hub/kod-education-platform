namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// The reactions and one page of comment threads under an article.
/// </summary>
/// <param name="Reactions">The reactions to the article.</param>
/// <param name="Comments">The top-level comments of the page, each with its whole branch of replies.</param>
/// <param name="CommentCount">The number of all comments and replies (deleted ones excluded).</param>
/// <param name="ThreadCount">The number of top-level comments (the pages are counted from it).</param>
/// <param name="Page">The 1-based page.</param>
/// <param name="PageSize">The number of threads per page.</param>
/// <param name="Sort">The order of the threads.</param>
/// <param name="CanReact">Whether the current user may react and comment at all (the article is published).</param>
/// <param name="CanComment">Whether comments are open.</param>
public sealed record NewsDiscussion(
    ReactionSummary Reactions,
    IReadOnlyList<NewsCommentDto> Comments,
    int CommentCount,
    int ThreadCount,
    int Page,
    int PageSize,
    CommentSort Sort,
    bool CanReact,
    bool CanComment);
