using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Users;

namespace KOD.Domain.Tickets;

/// <summary>
/// An entry in the history of a request: text with attachments written by the author or a handler, or a note about a
/// change of the state or the handler. Plain text (shown with its line breaks, never as HTML).
/// </summary>
public sealed class TicketMessage : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Body"/>.
    /// </summary>
    public const int BodyMaxLength = 10_000;

    /// <summary>
    /// The largest number of attachments of one message.
    /// </summary>
    public const int MaxAttachments = 10;

    /// <summary>
    /// The attachments.
    /// </summary>
    private readonly List<TicketAttachment> _attachments = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketMessage"/> class: text with attachments.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="authorId">The author.</param>
    /// <param name="body">The text.</param>
    /// <param name="attachments">The attachments.</param>
    /// <param name="moment">The current moment.</param>
    internal TicketMessage(Guid ticketId, Guid authorId, string? body, IReadOnlyCollection<TicketAttachment> attachments, DateTimeOffset moment)
    {
        if (attachments.Count > MaxAttachments)
        {
            throw new DomainException(Texts.T("До повідомлення можна додати не більше {0} файлів.", MaxAttachments));
        }

        var text = (body ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        if (text.Length == 0 && attachments.Count == 0)
        {
            throw new DomainException(Texts.T("Напишіть повідомлення або додайте файл."));
        }

        if (text.Length > BodyMaxLength)
        {
            throw new DomainException(Texts.T("Повідомлення задовге: не більше {0} символів.", BodyMaxLength));
        }

        TicketId = ticketId;
        AuthorId = authorId;
        Kind = TicketMessageKind.Comment;
        Body = text;
        CreatedAt = moment;
        foreach (var attachment in attachments)
        {
            attachment.AttachTo(this, _attachments.Count);
            _attachments.Add(attachment);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketMessage"/> class: a note about a change.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="actorId">Who made the change.</param>
    /// <param name="kind">The kind of the change.</param>
    /// <param name="status">The new state.</param>
    /// <param name="details">The name of the new handler.</param>
    /// <param name="moment">The current moment.</param>
    internal TicketMessage(Guid ticketId, Guid actorId, TicketMessageKind kind, TicketStatus? status, string? details, DateTimeOffset moment)
    {
        TicketId = ticketId;
        AuthorId = actorId;
        Kind = kind;
        Status = status;
        Body = details ?? string.Empty;
        CreatedAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketMessage"/> class for EF Core materialization.
    /// </summary>
    private TicketMessage()
    {
        Body = string.Empty;
    }

    /// <summary>
    /// Gets the request.
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// Gets the author (for a note, the person who made the change).
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    public TicketMessageKind Kind { get; private set; }

    /// <summary>
    /// Gets the new state of a <see cref="TicketMessageKind.StatusChanged"/> note.
    /// </summary>
    public TicketStatus? Status { get; private set; }

    /// <summary>
    /// Gets the text (the name of the new handler for an <see cref="TicketMessageKind.Assigned"/> note).
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Gets the moment it was written.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the attachments.
    /// </summary>
    public IReadOnlyCollection<TicketAttachment> Attachments => _attachments;
}
