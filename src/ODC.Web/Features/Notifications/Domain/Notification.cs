using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications.Domain;

/// <summary>
/// A notification of a user: what happened (<see cref="Kind"/>), who did it, what it concerns and where to look. The
/// texts are data (the title of a document, the start of a comment); the client translates the kind. Messages of one
/// chat collect in one unread notification (<see cref="Count"/>).
/// </summary>
public sealed class Notification : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Subject"/>.
    /// </summary>
    public const int SubjectMaxLength = 300;

    /// <summary>
    /// Maximum length of <see cref="Preview"/>.
    /// </summary>
    public const int PreviewMaxLength = 300;

    /// <summary>
    /// Maximum length of <see cref="Link"/>.
    /// </summary>
    public const int LinkMaxLength = 500;

    /// <summary>
    /// Maximum length of <see cref="Code"/>.
    /// </summary>
    public const int CodeMaxLength = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="Notification"/> class.
    /// </summary>
    /// <param name="userId">The recipient.</param>
    /// <param name="kind">What happened.</param>
    /// <param name="actorId">Who did it.</param>
    /// <param name="entityId">The document, article, chat or request concerned.</param>
    /// <param name="subject">The title of what is concerned.</param>
    /// <param name="preview">The start of the text (a comment, a message, a reason).</param>
    /// <param name="code">A machine value the client shows translated (the new state of a request).</param>
    /// <param name="link">The address of the page to open.</param>
    /// <param name="moment">The current moment.</param>
    public Notification(Guid userId, NotificationKind kind, Guid? actorId, Guid entityId, string subject, string? preview, string? code, string link, DateTimeOffset moment)
    {
        UserId = userId;
        Kind = kind;
        ActorId = actorId;
        EntityId = entityId;
        Subject = Cut(subject, SubjectMaxLength) ?? string.Empty;
        Preview = Cut(preview, PreviewMaxLength);
        Code = Cut(code, CodeMaxLength);
        ArgumentException.ThrowIfNullOrEmpty(link);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(link.Length, LinkMaxLength, nameof(link));
        Link = link;
        Count = 1;
        CreatedAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Notification"/> class for EF Core materialization.
    /// </summary>
    private Notification()
    {
        Subject = string.Empty;
        Link = string.Empty;
    }

    /// <summary>
    /// Gets the recipient.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets what happened.
    /// </summary>
    public NotificationKind Kind { get; private set; }

    /// <summary>
    /// Gets who did it (the author of the last message for a chat).
    /// </summary>
    public Guid? ActorId { get; private set; }

    /// <summary>
    /// Gets who did it.
    /// </summary>
    public User? Actor { get; private set; }

    /// <summary>
    /// Gets the document, article, chat or request concerned.
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// Gets the title of what is concerned.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Gets the start of the text (a comment, a message, a reason).
    /// </summary>
    public string? Preview { get; private set; }

    /// <summary>
    /// Gets a machine value the client shows translated (the new state of a request).
    /// </summary>
    public string? Code { get; private set; }

    /// <summary>
    /// Gets the address of the page to open.
    /// </summary>
    public string Link { get; private set; }

    /// <summary>
    /// Gets how many events the notification collects (messages of a chat).
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the moment of the last event.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the moment the recipient read it.
    /// </summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>
    /// Adds another event to an unread notification (one more message of the same chat).
    /// </summary>
    /// <param name="actorId">Who did it.</param>
    /// <param name="subject">The title (a chat may have been renamed).</param>
    /// <param name="preview">The start of the new text.</param>
    /// <param name="moment">The current moment.</param>
    public void Repeat(Guid? actorId, string subject, string? preview, DateTimeOffset moment)
    {
        ActorId = actorId;
        Subject = Cut(subject, SubjectMaxLength) ?? string.Empty;
        Preview = Cut(preview, PreviewMaxLength);
        Count++;
        CreatedAt = moment;
    }

    /// <summary>
    /// Marks the notification read.
    /// </summary>
    /// <param name="moment">The current moment.</param>
    /// <returns><see langword="true"/> when it was unread.</returns>
    public bool MarkRead(DateTimeOffset moment)
    {
        if (ReadAt is not null)
        {
            return false;
        }

        ReadAt = moment;
        return true;
    }

    /// <summary>
    /// Shortens a text to a length, ending it with an ellipsis.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The text, or <see langword="null"/> when blank.</returns>
    private static string? Cut(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 1).TrimEnd(), "…");
    }
}
