using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Users;

namespace KOD.Domain.News;

/// <summary>
/// A comment on a news article or a reply to any comment: the discussion is a tree of threads, like on Reddit. Every
/// comment knows the top-level comment of its thread (<see cref="RootId"/>), so a page of threads loads with one query.
/// </summary>
public sealed class NewsComment : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Body"/>.
    /// </summary>
    public const int BodyMaxLength = 2000;

    /// <summary>
    /// The deepest reply allowed (a top-level comment has depth 0).
    /// </summary>
    public const int MaxDepth = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsComment"/> class.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="authorId">The author.</param>
    /// <param name="parent">The comment this one replies to; <see langword="null"/> for a top-level comment.</param>
    /// <param name="body">The plain text.</param>
    public NewsComment(Guid articleId, Guid authorId, NewsComment? parent, string body)
    {
        if (parent is not null)
        {
            if (parent.ArticleId != articleId)
            {
                throw new DomainException(Texts.T("Коментар не знайдено."));
            }

            if (parent.IsDeleted)
            {
                throw new DomainException(Texts.T("Коментар видалено."));
            }

            if (parent.Depth >= MaxDepth)
            {
                throw new DomainException(Texts.T("Гілка обговорення надто глибока: відповідайте вище."));
            }
        }

        ArticleId = articleId;
        AuthorId = authorId;
        ParentId = parent?.Id;
        RootId = parent?.RootId ?? Id;
        Depth = parent is null ? 0 : parent.Depth + 1;
        Body = Guard.Text(body, BodyMaxLength, "Коментар");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsComment"/> class for EF Core materialization.
    /// </summary>
    private NewsComment()
    {
        Body = string.Empty;
    }

    /// <summary>
    /// Gets the article.
    /// </summary>
    public Guid ArticleId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets the comment this one replies to; <see langword="null"/> for a top-level comment.
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Gets the top-level comment of the thread (the comment itself for a top-level one).
    /// </summary>
    public Guid RootId { get; private set; }

    /// <summary>
    /// Gets the nesting level: 0 for a top-level comment, 1 for a reply to it and so on.
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// Gets the plain text (empty once deleted).
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Gets the moment of the last edit by the author.
    /// </summary>
    public DateTimeOffset? EditedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the comment was deleted. The row stays while it has replies, so the thread keeps its shape.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Replaces the text.
    /// </summary>
    /// <param name="body">The new text.</param>
    /// <param name="moment">The current moment.</param>
    public void Edit(string body, DateTimeOffset moment)
    {
        if (IsDeleted)
        {
            throw new DomainException(Texts.T("Коментар видалено."));
        }

        Body = Guard.Text(body, BodyMaxLength, "Коментар");
        EditedAt = moment;
    }

    /// <summary>
    /// Deletes the text and keeps a placeholder in the thread.
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        Body = string.Empty;
    }
}
