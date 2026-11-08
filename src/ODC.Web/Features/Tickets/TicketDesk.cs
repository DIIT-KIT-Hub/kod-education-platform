using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// What the request slices share: who may see a request (its author or a handler), who hears about a change, the list
/// projection and the files of messages.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="attachmentStore">Checks and stores the files of messages.</param>
internal sealed class TicketDesk(ApplicationDbContext db, ICurrentUser currentUser, AttachmentStore attachmentStore)
{
    /// <summary>
    /// The storage folder of request files.
    /// </summary>
    private const string StorageCategory = "tickets";

    /// <summary>
    /// Gets a value indicating whether the current user handles requests.
    /// </summary>
    public bool IsHandler => currentUser.Has(Permissions.Tickets.Manage);

    /// <summary>
    /// The route group <c>/api/v1/tickets</c>.
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapTickets(IEndpointRouteBuilder api) => api.MapGroup("tickets").WithTags("Tickets");

    /// <summary>
    /// The error of a request the current user cannot see.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Звернення не знайдено."));

    /// <summary>
    /// Builds the address of a request file.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="thumbnail">Whether the thumbnail is meant.</param>
    /// <returns>The address.</returns>
    public static string AttachmentUrl(Guid ticketId, Guid attachmentId, bool thumbnail)
        => $"/api/v1/tickets/{ticketId}/attachments/{attachmentId}{(thumbnail ? "/thumb" : string.Empty)}";

    /// <summary>
    /// Applies the search and the filters and sorts a request list.
    /// </summary>
    /// <param name="tickets">The requests.</param>
    /// <param name="query">The search, filters and sorting.</param>
    /// <param name="me">The current user.</param>
    /// <returns>The ordered query.</returns>
    public static IQueryable<Ticket> Filter(IQueryable<Ticket> tickets, TicketQuery query, Guid me)
    {
        if (query.Status is { } status)
        {
            tickets = tickets.Where(t => t.Status == status);
        }

        if (query.Category is { } category)
        {
            tickets = tickets.Where(t => t.Category == category);
        }

        if (query.OpenOnly == true)
        {
            tickets = tickets.Where(static t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        }

        if (query.AssignedToMe == true)
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
            "number" => tickets.OrderByDirection(static t => t.Number, query.IsDescending),
            "created" => tickets.OrderByDirection(static t => t.CreatedAt, query.IsDescending),
            "priority" => tickets.OrderByDirection(
                    static t => t.Priority == TicketPriority.Critical ? 3 : t.Priority == TicketPriority.High ? 2 : t.Priority == TicketPriority.Normal ? 1 : 0,
                    query.IsDescending)
                .ThenByDescending(static t => t.LastActivityAt),
            "status" => tickets.OrderByDirection(
                    static t => t.Status == TicketStatus.New ? 0 : t.Status == TicketStatus.InProgress ? 1 : t.Status == TicketStatus.WaitingForAuthor ? 2 : t.Status == TicketStatus.Resolved ? 3 : 4,
                    query.IsDescending)
                .ThenByDescending(static t => t.LastActivityAt),
            _ => tickets.OrderByDirection(static t => t.LastActivityAt, query.SortBy != "updated" || query.IsDescending),
        };
    }

    /// <summary>
    /// Projects requests for a list.
    /// </summary>
    /// <param name="tickets">The ordered requests.</param>
    /// <returns>The query.</returns>
    public IQueryable<TicketListItem> Project(IQueryable<Ticket> tickets) => tickets.Select(t => new TicketListItem(
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
    /// <param name="id">The ticket.</param>
    /// <param name="me">The current user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked request, or <see langword="null"/>.</returns>
    public async Task<Ticket?> VisibleAsync(Guid id, Guid me, CancellationToken cancellationToken)
    {
        var handler = IsHandler;
        return await db.Tickets.FirstOrDefaultAsync(t => t.Id == id && (handler || t.AuthorId == me), cancellationToken);
    }

    /// <summary>
    /// Gets the full name of a person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The name.</returns>
    public async Task<string> NameOfAsync(Guid userId, CancellationToken cancellationToken)
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
    public async Task<Result<IReadOnlyCollection<TicketAttachment>>> StoreAsync(IReadOnlyList<FileUpload> uploads, List<string> stored, CancellationToken cancellationToken)
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
    /// Deletes stored files after a failed message.
    /// </summary>
    /// <param name="stored">The keys of the stored files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the files are gone.</returns>
    public Task DeleteAsync(List<string> stored, CancellationToken cancellationToken) => attachmentStore.DeleteAsync(stored, cancellationToken);

    /// <summary>
    /// Loads the history of a request with its files.
    /// </summary>
    /// <param name="ticketId">The request.</param>
    /// <param name="authorId">The author of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entries, oldest first.</returns>
    public async Task<List<TicketMessageDto>> LoadMessagesAsync(Guid ticketId, Guid authorId, CancellationToken cancellationToken)
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
