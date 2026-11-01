using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Application.Features.Notifications;
using KOD.Domain.Access;
using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Notifications;
using KOD.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Tickets;

/// <summary>
/// Default <see cref="ITicketService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="attachmentStore">Checks and stores the files of messages.</param>
/// <param name="files">The private file storage.</param>
/// <param name="notifier">Notifies the people concerned.</param>
/// <param name="mail">E-mails the handlers about new requests.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class TicketService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    AttachmentStore attachmentStore,
    IFileStorage files,
    INotifier notifier,
    TicketMail mail,
    TimeProvider timeProvider) : ITicketService
{
    /// <summary>
    /// The storage folder of request files.
    /// </summary>
    private const string StorageCategory = "tickets";

    /// <summary>
    /// Gets a value indicating whether the current user handles requests.
    /// </summary>
    private bool IsHandler => currentUser.Has(Permissions.Tickets.Manage);

    /// <inheritdoc />
    public async Task<PagedResult<TicketListItem>> ListMineAsync(TicketQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (currentUser.Id is not { } me)
        {
            return PagedResult<TicketListItem>.Empty(query);
        }

        return await Project(Filter(db.Tickets.AsNoTracking().Where(t => t.AuthorId == me), query, me)).ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<TicketListItem>>> ListAllAsync(TicketQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (currentUser.Id is not { } me || !IsHandler)
        {
            return Error.Forbidden();
        }

        return await Project(Filter(db.Tickets.AsNoTracking(), query, me)).ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<TicketDetails>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return NotFound();
        }

        var handler = IsHandler;
        var row = await db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == id && (handler || t.AuthorId == me))
            .Select(static t => new
            {
                t.Id,
                t.Number,
                t.Subject,
                t.Category,
                t.Priority,
                t.Status,
                t.AuthorId,
                Author = new UserRef(t.AuthorId, t.Author!.LastName + " " + t.Author.FirstName + (t.Author.MiddleName != null ? " " + t.Author.MiddleName : string.Empty), t.Author.PhotoId),
                t.Author.Email,
                Title = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                RoleName = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                UnitName = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault(),
                Assignee = t.AssigneeId == null
                    ? null
                    : new UserRef(t.Assignee!.Id, t.Assignee.LastName + " " + t.Assignee.FirstName + (t.Assignee.MiddleName != null ? " " + t.Assignee.MiddleName : string.Empty), t.Assignee.PhotoId),
                t.CreatedAt,
                t.LastActivityAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        var messages = await LoadMessagesAsync(row.Id, row.AuthorId, cancellationToken);
        await notifier.MarkReadAsync(me, id, cancellationToken);

        var isMine = row.AuthorId == me;
        var open = row.Status != TicketStatus.Closed;
        return new TicketDetails(
            row.Id,
            row.Number,
            row.Subject,
            row.Category,
            row.Priority,
            row.Status,
            row.Author,
            handler ? StoredText.PositionInUnit(row.Title, row.RoleName, row.UnitName) : null,
            handler ? row.Email : null,
            row.Assignee,
            row.CreatedAt,
            row.LastActivityAt,
            messages,
            isMine,
            handler,
            open,
            open && (handler || isMine));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> SubmitAsync(SubmitTicket command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var stored = new List<string>();
        try
        {
            var attachments = await StoreAsync(command.Files, stored, cancellationToken);
            if (!attachments.IsSuccess)
            {
                await attachmentStore.DeleteAsync(stored, cancellationToken);
                return attachments.Error;
            }

            var (ticket, description) = Ticket.Submit(me, command.Category, command.Priority, command.Subject, command.Description, attachments.Value, timeProvider.GetUtcNow());
            db.Tickets.Add(ticket);
            db.TicketMessages.Add(description);
            await db.SaveChangesAsync(cancellationToken);

            var handlers = await db.UsersWith(Permissions.Tickets.Manage).Select(static u => u.Id).ToListAsync(cancellationToken);
            await notifier.NotifyAsync(Draft(NotificationKind.TicketCreated, me, ticket, description.Body), handlers, cancellationToken);
            await mail.NewTicketAsync(ticket, description, cancellationToken);
            return ticket.Id;
        }
        catch (DomainException exception)
        {
            await attachmentStore.DeleteAsync(stored, cancellationToken);
            return Error.Validation(exception.Message);
        }
        catch
        {
            await attachmentStore.DeleteAsync(stored, CancellationToken.None);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result> CommentAsync(Guid id, string? body, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (currentUser.Id is not { } me)
        {
            return NotFound();
        }

        var ticket = await VisibleAsync(id, me, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!ticket.IsOpen)
        {
            return Error.Validation(Texts.T("Звернення закрито: відповісти на нього не можна."));
        }

        var stored = new List<string>();
        try
        {
            var attachments = await StoreAsync(files, stored, cancellationToken);
            if (!attachments.IsSuccess)
            {
                await attachmentStore.DeleteAsync(stored, cancellationToken);
                return attachments.Error;
            }

            var entries = ticket.Comment(me, await NameOfAsync(me, cancellationToken), IsHandler, body, attachments.Value, timeProvider.GetUtcNow());
            db.TicketMessages.AddRange(entries);
            await db.SaveChangesAsync(cancellationToken);

            var message = entries[^1];
            var preview = message.Body.Length > 0 ? message.Body : null;
            await notifier.NotifyAsync(Draft(NotificationKind.TicketReply, me, ticket, preview), await CounterpartsAsync(ticket, me, cancellationToken), cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            await attachmentStore.DeleteAsync(stored, cancellationToken);
            return Error.Validation(exception.Message);
        }
        catch
        {
            await attachmentStore.DeleteAsync(stored, CancellationToken.None);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangeStatusAsync(Guid id, TicketStatus status, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return NotFound();
        }

        var ticket = await VisibleAsync(id, me, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        try
        {
            db.TicketMessages.Add(ticket.ChangeStatus(me, IsHandler, status, timeProvider.GetUtcNow()));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }

        var draft = Draft(NotificationKind.TicketStatusChanged, me, ticket, null) with { Code = status.ToString() };
        await notifier.NotifyAsync(draft, await CounterpartsAsync(ticket, me, cancellationToken), cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> AssignAsync(Guid id, Guid? assigneeId, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me || !IsHandler)
        {
            return Error.Forbidden();
        }

        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        string? name = null;
        if (assigneeId is { } assignee)
        {
            if (!await db.UsersWith(Permissions.Tickets.Manage).AnyAsync(u => u.Id == assignee, cancellationToken))
            {
                return Error.Validation(Texts.T("Звернення можна призначити лише тому, хто опрацьовує звернення."));
            }

            name = await NameOfAsync(assignee, cancellationToken);
        }

        var note = ticket.Assign(me, assigneeId, name, timeProvider.GetUtcNow());
        if (note is null)
        {
            return Result.Success();
        }

        db.TicketMessages.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        if (assigneeId is { } handler)
        {
            await notifier.NotifyAsync(Draft(NotificationKind.TicketAssigned, me, ticket, null), [handler], cancellationToken);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ClassifyAsync(Guid id, TicketCategory category, TicketPriority priority, CancellationToken cancellationToken)
    {
        if (!IsHandler)
        {
            return Error.Forbidden();
        }

        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        try
        {
            if (ticket.Classify(category, priority))
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserRef>>> ListHandlersAsync(CancellationToken cancellationToken)
    {
        if (!IsHandler)
        {
            return Error.Forbidden();
        }

        var handlers = await db.UsersWith(Permissions.Tickets.Manage)
            .AsNoTracking()
            .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
            .Select(static u => new UserRef(u.Id, u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty), u.PhotoId))
            .ToListAsync(cancellationToken);
        return handlers;
    }

    /// <inheritdoc />
    public async Task<Result<DownloadableFile>> OpenAttachmentAsync(Guid id, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return NotFound();
        }

        var handler = IsHandler;
        var attachment = await db.TicketAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachmentId && a.TicketId == id && db.Tickets.Any(t => t.Id == id && (handler || t.AuthorId == me)))
            .FirstOrDefaultAsync(cancellationToken);
        if (attachment is null)
        {
            return Error.NotFound(Texts.T("Файл не знайдено."));
        }

        var key = thumbnail && attachment.ThumbnailKey is { } small ? small : attachment.StorageKey;
        var name = attachment.ThumbnailKey is null ? attachment.FileName : Path.ChangeExtension(attachment.FileName, ".avif");
        return new DownloadableFile(await files.OpenReadAsync(key, cancellationToken), name, attachment.ContentType);
    }

    /// <summary>
    /// The error of a request the current user cannot see.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error NotFound() => Error.NotFound(Texts.T("Звернення не знайдено."));

    /// <summary>
    /// Builds the address of a request file.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="thumbnail">Whether the thumbnail is meant.</param>
    /// <returns>The address.</returns>
    private static string AttachmentUrl(Guid ticketId, Guid attachmentId, bool thumbnail)
        => $"/api/v1/tickets/{ticketId}/attachments/{attachmentId}{(thumbnail ? "/thumb" : string.Empty)}";

    /// <summary>
    /// Builds a notification about a request.
    /// </summary>
    /// <param name="kind">What happened.</param>
    /// <param name="actorId">Who did it.</param>
    /// <param name="ticket">The request.</param>
    /// <param name="preview">The start of the text.</param>
    /// <returns>The draft.</returns>
    private static NotificationDraft Draft(NotificationKind kind, Guid actorId, Ticket ticket, string? preview)
        => new(kind, actorId, ticket.Id, $"№ {ticket.Number} · {ticket.Subject}", preview, $"/requests/{ticket.Id}");

    /// <summary>
    /// Applies the search and the filters and sorts a request list.
    /// </summary>
    /// <param name="tickets">The requests.</param>
    /// <param name="query">The search, filters and sorting.</param>
    /// <param name="me">The current user.</param>
    /// <returns>The ordered query.</returns>
    private static IQueryable<Ticket> Filter(IQueryable<Ticket> tickets, TicketQuery query, Guid me)
    {
        if (query.Status is { } status)
        {
            tickets = tickets.Where(t => t.Status == status);
        }

        if (query.Category is { } category)
        {
            tickets = tickets.Where(t => t.Category == category);
        }

        if (query.OpenOnly)
        {
            tickets = tickets.Where(static t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        }

        if (query.AssignedToMe)
        {
            tickets = tickets.Where(t => t.AssigneeId == me);
        }

        if (query.Search is { } search)
        {
            var term = search.TrimStart('#', '№').Trim();
            tickets = int.TryParse(term, out var number)
                ? tickets.Where(t => t.Number == number || t.Subject.Contains(term))
                : tickets.Where(t => t.Subject.Contains(term) || t.Author!.LastName.Contains(term) || t.Author.FirstName.Contains(term) || t.Author.Email!.Contains(term));
        }

        // Enums are stored as names: the urgency and the state are ordered by their meaning, not alphabetically.
        return query.SortBy switch
        {
            "number" => tickets.OrderByDirection(static t => t.Number, query.Descending),
            "created" => tickets.OrderByDirection(static t => t.CreatedAt, query.Descending),
            "priority" => tickets.OrderByDirection(
                    static t => t.Priority == TicketPriority.Critical ? 3 : t.Priority == TicketPriority.High ? 2 : t.Priority == TicketPriority.Normal ? 1 : 0,
                    query.Descending)
                .ThenByDescending(static t => t.LastActivityAt),
            "status" => tickets.OrderByDirection(
                    static t => t.Status == TicketStatus.New ? 0 : t.Status == TicketStatus.InProgress ? 1 : t.Status == TicketStatus.WaitingForAuthor ? 2 : t.Status == TicketStatus.Resolved ? 3 : 4,
                    query.Descending)
                .ThenByDescending(static t => t.LastActivityAt),
            _ => tickets.OrderByDirection(static t => t.LastActivityAt, query.SortBy != "updated" || query.Descending),
        };
    }

    /// <summary>
    /// Projects requests for a list.
    /// </summary>
    /// <param name="tickets">The ordered requests.</param>
    /// <returns>The query.</returns>
    private IQueryable<TicketListItem> Project(IQueryable<Ticket> tickets) => tickets.Select(t => new TicketListItem(
        t.Id,
        t.Number,
        t.Subject,
        t.Category,
        t.Priority,
        t.Status,
        new UserRef(t.AuthorId, t.Author!.LastName + " " + t.Author.FirstName + (t.Author.MiddleName != null ? " " + t.Author.MiddleName : string.Empty), t.Author.PhotoId),
        t.AssigneeId == null
            ? null
            : new UserRef(t.Assignee!.Id, t.Assignee.LastName + " " + t.Assignee.FirstName + (t.Assignee.MiddleName != null ? " " + t.Assignee.MiddleName : string.Empty), t.Assignee.PhotoId),
        t.CreatedAt,
        t.LastActivityAt,
        db.TicketMessages.Count(m => m.TicketId == t.Id && m.Kind == TicketMessageKind.Comment),
        db.TicketMessages
            .Where(m => m.TicketId == t.Id && m.Kind == TicketMessageKind.Comment)
            .OrderByDescending(static m => m.CreatedAt)
            .Select(static m => m.AuthorId)
            .FirstOrDefault() == t.AuthorId));

    /// <summary>
    /// Loads a request for a change when the current user may see it (its author or a handler).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="me">The current user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked request, or <see langword="null"/>.</returns>
    private async Task<Ticket?> VisibleAsync(Guid id, Guid me, CancellationToken cancellationToken)
    {
        var handler = IsHandler;
        return await db.Tickets.FirstOrDefaultAsync(t => t.Id == id && (handler || t.AuthorId == me), cancellationToken);
    }

    /// <summary>
    /// Finds who should hear about a message or a change: the author when a handler acted; the handler of the request (or
    /// every handler while nobody has it) when the author acted.
    /// </summary>
    /// <param name="ticket">The request.</param>
    /// <param name="me">Who acted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    private async Task<IReadOnlyCollection<Guid>> CounterpartsAsync(Ticket ticket, Guid me, CancellationToken cancellationToken)
    {
        if (me != ticket.AuthorId)
        {
            return ticket.AssigneeId is { } assignee && assignee != me ? [ticket.AuthorId, assignee] : [ticket.AuthorId];
        }

        return ticket.AssigneeId is { } handler
            ? [handler]
            : await db.UsersWith(Permissions.Tickets.Manage).Select(static u => u.Id).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the full name of a person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The name.</returns>
    private async Task<string> NameOfAsync(Guid userId, CancellationToken cancellationToken)
        => await db.Users
            .Where(u => u.Id == userId)
            .Select(static u => u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty))
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

    /// <summary>
    /// Checks and stores the files of a message.
    /// </summary>
    /// <param name="uploads">The uploaded files.</param>
    /// <param name="stored">Collects the keys of the stored files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The attachments.</returns>
    private async Task<Result<IReadOnlyCollection<TicketAttachment>>> StoreAsync(IReadOnlyList<FileUpload> uploads, List<string> stored, CancellationToken cancellationToken)
    {
        var result = await attachmentStore.StoreAsync(
            uploads,
            StorageCategory,
            TicketMessage.MaxAttachments,
            static extension => Texts.T("Файли типу «{0}» не можна додати до звернення.", extension),
            stored,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        return Result<IReadOnlyCollection<TicketAttachment>>.Success(
            [.. result.Value.Select(static f => new TicketAttachment(f.FileName, f.ContentType, f.Size, f.StorageKey, f.ThumbnailKey, f.Width, f.Height))]);
    }

    /// <summary>
    /// Loads the history of a request with its files.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="authorId">The author of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entries, oldest first.</returns>
    private async Task<List<TicketMessageDto>> LoadMessagesAsync(Guid ticketId, Guid authorId, CancellationToken cancellationToken)
    {
        var rows = await db.TicketMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderBy(static m => m.CreatedAt)
            .ThenBy(static m => m.Kind == TicketMessageKind.Comment ? 0 : 1)
            .Select(static m => new
            {
                m.Id,
                m.Kind,
                m.AuthorId,
                Author = new UserRef(m.AuthorId, m.Author!.LastName + " " + m.Author.FirstName + (m.Author.MiddleName != null ? " " + m.Author.MiddleName : string.Empty), m.Author.PhotoId),
                m.Body,
                m.Status,
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);
        var attachments = (await db.TicketAttachments
                .AsNoTracking()
                .Where(a => a.TicketId == ticketId)
                .OrderBy(static a => a.Position)
                .ToListAsync(cancellationToken))
            .ToLookup(static a => a.MessageId);

        return rows.ConvertAll(r => new TicketMessageDto(
            r.Id,
            r.Kind,
            r.Author,
            r.AuthorId == authorId,
            r.Body,
            r.Status,
            r.CreatedAt,
            attachments[r.Id].Select(a => new TicketAttachmentDto(
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.Size,
                    a.ThumbnailKey is not null,
                    a.Width,
                    a.Height,
                    AttachmentUrl(ticketId, a.Id, thumbnail: false),
                    a.ThumbnailKey is null ? null : AttachmentUrl(ticketId, a.Id, thumbnail: true)))
                .ToList()));
    }
}
