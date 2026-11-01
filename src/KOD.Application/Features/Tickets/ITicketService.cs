using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Domain.Tickets;

namespace KOD.Application.Features.Tickets;

/// <summary>
/// Requests of users (bugs, suggestions, questions): every signed-in user submits requests and talks with the handlers in
/// them; the people with <c>tickets.manage</c> see all requests, answer, assign and change their state. The people
/// concerned get notifications.
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Lists the requests of the current user.
    /// </summary>
    /// <param name="query">The search, filters, sorting and paging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    Task<PagedResult<TicketListItem>> ListMineAsync(TicketQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the requests of all users (handlers).
    /// </summary>
    /// <param name="query">The search, filters, sorting and paging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    Task<Result<PagedResult<TicketListItem>>> ListAllAsync(TicketQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a request with its history (its author or a handler) and marks the notifications about it read.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request with its history.</returns>
    Task<Result<TicketDetails>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Submits a request and notifies the handlers.
    /// </summary>
    /// <param name="command">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new request.</returns>
    Task<Result<Guid>> SubmitAsync(SubmitTicket command, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a message in a request.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="body">The text.</param>
    /// <param name="files">The attached files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> CommentAsync(Guid id, string? body, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the state of a request (a handler; the author may close their own).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="status">The new state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ChangeStatusAsync(Guid id, TicketStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// Assigns a request to a handler or releases it (handlers).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="assigneeId">The handler, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> AssignAsync(Guid id, Guid? assigneeId, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the category and the priority of a request (handlers).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="category">What it is about.</param>
    /// <param name="priority">How urgent it is.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ClassifyAsync(Guid id, TicketCategory category, TicketPriority priority, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the people who handle requests (the choice of a handler).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    Task<Result<IReadOnlyList<UserRef>>> ListHandlersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a file of a request (its author or a handler).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="thumbnail">Whether the thumbnail of a photo is meant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content of the file.</returns>
    Task<Result<DownloadableFile>> OpenAttachmentAsync(Guid id, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken);
}
