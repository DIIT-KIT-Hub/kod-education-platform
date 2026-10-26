using KOD.Domain.Common;

namespace KOD.Domain.News;

/// <summary>
/// A reader's reaction to an article (<see cref="CommentId"/> is <see langword="null"/>) or to one of its comments.
/// A reader has at most one reaction per target.
/// </summary>
public sealed class NewsReaction : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewsReaction"/> class.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="commentId">The comment, or <see langword="null"/> for the article itself.</param>
    /// <param name="userId">The reader.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="moment">The current moment.</param>
    public NewsReaction(Guid articleId, Guid? commentId, Guid userId, ReactionKind kind, DateTimeOffset moment)
    {
        ArticleId = articleId;
        CommentId = commentId;
        UserId = userId;
        Kind = kind;
        ReactedAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsReaction"/> class for EF Core materialization.
    /// </summary>
    private NewsReaction()
    {
    }

    /// <summary>
    /// Gets the article.
    /// </summary>
    public Guid ArticleId { get; private set; }

    /// <summary>
    /// Gets the comment; <see langword="null"/> for a reaction to the article itself.
    /// </summary>
    public Guid? CommentId { get; private set; }

    /// <summary>
    /// Gets the reader.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the emotion.
    /// </summary>
    public ReactionKind Kind { get; private set; }

    /// <summary>
    /// Gets the moment of the reaction.
    /// </summary>
    public DateTimeOffset ReactedAt { get; private set; }

    /// <summary>
    /// Replaces the emotion.
    /// </summary>
    /// <param name="kind">The new emotion.</param>
    /// <param name="moment">The current moment.</param>
    public void Change(ReactionKind kind, DateTimeOffset moment)
    {
        Kind = kind;
        ReactedAt = moment;
    }
}
