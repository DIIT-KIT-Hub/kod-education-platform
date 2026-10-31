namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// The order of the comment threads under an article.
/// </summary>
public enum CommentSort
{
    /// <summary>
    /// The threads with the most reactions and replies first, then the newest.
    /// </summary>
    Best,

    /// <summary>
    /// The newest threads first.
    /// </summary>
    Newest,

    /// <summary>
    /// The oldest threads first (the discussion in the order it happened).
    /// </summary>
    Oldest,
}
