using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Users;

namespace KOD.Domain.Tickets;

/// <summary>
/// A request of a user to the people who look after the cabinet: a bug, a suggestion, a question. It has a number, a
/// state and a history of messages (<see cref="TicketMessage"/>) between the author and the handlers. The handlers
/// change the state; the author may only withdraw it, and an answer of the author opens a resolved request again.
/// </summary>
public sealed class Ticket : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Subject"/>.
    /// </summary>
    public const int SubjectMaxLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ticket"/> class.
    /// </summary>
    /// <param name="authorId">The author.</param>
    /// <param name="category">What it is about.</param>
    /// <param name="priority">How urgent it is.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="moment">The current moment.</param>
    private Ticket(Guid authorId, TicketCategory category, TicketPriority priority, string subject, DateTimeOffset moment)
    {
        if (!Enum.IsDefined(category) || !Enum.IsDefined(priority))
        {
            throw new DomainException(Texts.T("Оберіть категорію та пріоритет звернення."));
        }

        AuthorId = authorId;
        Category = category;
        Priority = priority;
        Subject = Guard.Text(subject, SubjectMaxLength, "Тема");
        Status = TicketStatus.New;
        CreatedAt = moment;
        LastActivityAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ticket"/> class for EF Core materialization.
    /// </summary>
    private Ticket()
    {
        Subject = string.Empty;
    }

    /// <summary>
    /// Gets the number shown to people (1, 2, 3…, given by the database).
    /// </summary>
    public int Number { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets what it is about.
    /// </summary>
    public TicketCategory Category { get; private set; }

    /// <summary>
    /// Gets how urgent it is.
    /// </summary>
    public TicketPriority Priority { get; private set; }

    /// <summary>
    /// Gets the state.
    /// </summary>
    public TicketStatus Status { get; private set; }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Gets the handler who works on it.
    /// </summary>
    public Guid? AssigneeId { get; private set; }

    /// <summary>
    /// Gets the handler who works on it.
    /// </summary>
    public User? Assignee { get; private set; }

    /// <summary>
    /// Gets the moment of the last message or change (lists are sorted by it).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// Gets the moment it was resolved or closed.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether anyone may still write in it.
    /// </summary>
    public bool IsOpen => Status != TicketStatus.Closed;

    /// <summary>
    /// Submits a new request with its description.
    /// </summary>
    /// <param name="authorId">The author.</param>
    /// <param name="category">What it is about.</param>
    /// <param name="priority">How urgent it is.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="description">The description.</param>
    /// <param name="attachments">The files of the description.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The request and its first message.</returns>
    public static (Ticket Ticket, TicketMessage Description) Submit(
        Guid authorId,
        TicketCategory category,
        TicketPriority priority,
        string subject,
        string description,
        IReadOnlyCollection<TicketAttachment> attachments,
        DateTimeOffset moment)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        var ticket = new Ticket(authorId, category, priority, subject, moment);
        var text = Guard.Text(description, TicketMessage.BodyMaxLength, "Опис");
        return (ticket, new TicketMessage(ticket.Id, authorId, text, attachments, moment));
    }

    /// <summary>
    /// Adds a message. An answer of the author brings a request that waits for them or is resolved back to work; the first
    /// answer of a handler takes a new request into work and assigns it to them when nobody has it.
    /// </summary>
    /// <param name="userId">The writer.</param>
    /// <param name="userName">The full name of the writer (for the note of an assignment).</param>
    /// <param name="isHandler">Whether the writer handles requests.</param>
    /// <param name="body">The text.</param>
    /// <param name="attachments">The files.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The new entries of the history: notes of automatic changes first, the message last.</returns>
    public IReadOnlyList<TicketMessage> Comment(Guid userId, string userName, bool isHandler, string? body, IReadOnlyCollection<TicketAttachment> attachments, DateTimeOffset moment)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        EnsureCanWrite(userId, isHandler);

        var entries = new List<TicketMessage>();
        var message = new TicketMessage(Id, userId, body, attachments, moment);
        if (userId == AuthorId)
        {
            if (Status is TicketStatus.WaitingForAuthor or TicketStatus.Resolved)
            {
                entries.Add(SetStatus(userId, TicketStatus.InProgress, moment));
            }
        }
        else if (Status == TicketStatus.New)
        {
            if (AssigneeId is null)
            {
                entries.Add(SetAssignee(userId, userId, userName, moment));
            }

            entries.Add(SetStatus(userId, TicketStatus.InProgress, moment));
        }

        entries.Add(message);
        LastActivityAt = moment;
        return entries;
    }

    /// <summary>
    /// Changes the state. A handler sets any state (also opens a closed request again); the author may only close their
    /// own open request.
    /// </summary>
    /// <param name="userId">Who changes it.</param>
    /// <param name="isHandler">Whether they handle requests.</param>
    /// <param name="status">The new state.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The note for the history.</returns>
    public TicketMessage ChangeStatus(Guid userId, bool isHandler, TicketStatus status, DateTimeOffset moment)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException(Texts.T("Невідомий стан звернення."));
        }

        if (!isHandler && (userId != AuthorId || status != TicketStatus.Closed))
        {
            throw new DomainException(Texts.T("Змінювати стан звернення можуть лише ті, хто опрацьовує звернення."));
        }

        if (status == Status)
        {
            throw new DomainException(Texts.T("Звернення вже має цей стан."));
        }

        var note = SetStatus(userId, status, moment);
        LastActivityAt = moment;
        return note;
    }

    /// <summary>
    /// Assigns the request to a handler or releases it.
    /// </summary>
    /// <param name="userId">Who assigns it.</param>
    /// <param name="assigneeId">The handler, or <see langword="null"/> to release it.</param>
    /// <param name="assigneeName">The full name of the handler.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The note for the history, or <see langword="null"/> when nothing changed.</returns>
    public TicketMessage? Assign(Guid userId, Guid? assigneeId, string? assigneeName, DateTimeOffset moment)
    {
        if (assigneeId == AssigneeId)
        {
            return null;
        }

        var note = SetAssignee(userId, assigneeId, assigneeName, moment);
        LastActivityAt = moment;
        return note;
    }

    /// <summary>
    /// Changes the category and the priority (a handler sorts requests).
    /// </summary>
    /// <param name="category">What it is about.</param>
    /// <param name="priority">How urgent it is.</param>
    /// <returns><see langword="true"/> when something changed.</returns>
    public bool Classify(TicketCategory category, TicketPriority priority)
    {
        if (!Enum.IsDefined(category) || !Enum.IsDefined(priority))
        {
            throw new DomainException(Texts.T("Оберіть категорію та пріоритет звернення."));
        }

        if (category == Category && priority == Priority)
        {
            return false;
        }

        Category = category;
        Priority = priority;
        return true;
    }

    /// <summary>
    /// Checks that a person may write in the request now.
    /// </summary>
    /// <param name="userId">The writer.</param>
    /// <param name="isHandler">Whether the writer handles requests.</param>
    private void EnsureCanWrite(Guid userId, bool isHandler)
    {
        if (userId != AuthorId && !isHandler)
        {
            throw new DomainException(Texts.T("Звернення не знайдено."));
        }

        if (!IsOpen)
        {
            throw new DomainException(Texts.T("Звернення закрито: відповісти на нього не можна."));
        }
    }

    /// <summary>
    /// Sets the state and writes the note.
    /// </summary>
    /// <param name="userId">Who changes it.</param>
    /// <param name="status">The new state.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The note.</returns>
    private TicketMessage SetStatus(Guid userId, TicketStatus status, DateTimeOffset moment)
    {
        Status = status;
        ClosedAt = status is TicketStatus.Resolved or TicketStatus.Closed ? moment : null;
        return new TicketMessage(Id, userId, TicketMessageKind.StatusChanged, status, null, moment);
    }

    /// <summary>
    /// Sets the handler and writes the note.
    /// </summary>
    /// <param name="userId">Who assigns it.</param>
    /// <param name="assigneeId">The handler.</param>
    /// <param name="assigneeName">The full name of the handler.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The note.</returns>
    private TicketMessage SetAssignee(Guid userId, Guid? assigneeId, string? assigneeName, DateTimeOffset moment)
    {
        AssigneeId = assigneeId;
        return new TicketMessage(Id, userId, TicketMessageKind.Assigned, null, assigneeId is null ? null : assigneeName, moment);
    }
}
