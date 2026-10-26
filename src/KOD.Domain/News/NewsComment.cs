using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Users;

namespace KOD.Domain.News;

/// <summary>
/// A comment on a news article or a reply to a comment (one level of replies).
/// </summary>
public sealed class NewsComment : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Body"/>.
    /// </summary>
    public const int BodyMaxLength = 2000;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsComment"/> class.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="authorId">The author.</param>
    /// <param name="parentId">The comment this one replies to; <see langword="null"/> for a top-level comment.</param>
    /// <param name="body">The plain text.</param>
    public NewsComment(Guid articleId, Guid authorId, Guid? parentId, string body)
    {
        ArticleId = articleId;
        AuthorId = authorId;
        ParentId = parentId;
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
