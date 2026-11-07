using Microsoft.AspNetCore.Mvc;
using ODC.Application.Common.Files;
using ODC.Application.Common.Models;
using ODC.Application.Common.Paging;
using ODC.Application.Features.Tickets;
using ODC.Domain.Access;
using ODC.Web.Api.Contracts;
using ODC.Web.Security;

namespace ODC.Web.Api;

/// <summary>
/// Requests of users (bugs, suggestions, questions): every signed-in user submits requests and writes in their own; the
/// people with <c>tickets.manage</c> see all of them, answer, assign and change their state.
/// </summary>
/// <param name="tickets">The request service.</param>
[Route("api/v1/tickets")]
public sealed class TicketsController(ITicketService tickets) : ApiControllerBase
{
    /// <summary>
    /// The caching policy of photos: an attachment never changes under its identifier.
    /// </summary>
    private const string PhotoCacheControl = "private, max-age=31536000, immutable";

    /// <summary>
    /// Lists the requests of the current user.
    /// </summary>
    /// <param name="query">The search, filters, sorting and paging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<TicketListItem>>> Mine([FromQuery] TicketQuery query, CancellationToken cancellationToken)
        => Ok(await tickets.ListMineAsync(query, cancellationToken));

    /// <summary>
    /// Lists the requests of all users.
    /// </summary>
    /// <param name="query">The search, filters, sorting and paging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    [HttpGet]
    [RequirePermission(Permissions.Tickets.Manage)]
    public async Task<ActionResult<PagedResult<TicketListItem>>> List([FromQuery] TicketQuery query, CancellationToken cancellationToken)
        => FromResult(await tickets.ListAllAsync(query, cancellationToken));

    /// <summary>
    /// Lists the people who handle requests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    [HttpGet("handlers")]
    [RequirePermission(Permissions.Tickets.Manage)]
    public async Task<ActionResult<IReadOnlyList<UserRef>>> Handlers(CancellationToken cancellationToken)
        => FromResult(await tickets.ListHandlersAsync(cancellationToken));

    /// <summary>
    /// Gets a request with its history.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request with its history.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await tickets.GetAsync(id, cancellationToken));

    /// <summary>
    /// Submits a request (multipart: the fields and up to ten files).
    /// </summary>
    /// <param name="form">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the request.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxChatMessageSize + (1024 * 1024))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketIdResponse>> Submit([FromForm] SubmitTicketForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var result = await WithUploadsAsync(form.Files, uploads => tickets.SubmitAsync(
            new SubmitTicket(form.Category, form.Priority, form.Subject, form.Description, uploads),
            cancellationToken));
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, new TicketIdResponse(result.Value))
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Writes a message in a request (multipart: the text and up to ten files).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="form">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("{id:guid}/messages")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxChatMessageSize + (1024 * 1024))]
    public async Task<ActionResult> Comment(Guid id, [FromForm] TicketMessageForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        return FromResult(await WithUploadsAsync(form.Files, uploads => tickets.CommentAsync(id, form.Body, uploads, cancellationToken)));
    }

    /// <summary>
    /// Changes the state of a request (a handler; the author may close their own).
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="request">The new state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult> ChangeStatus(Guid id, TicketStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await tickets.ChangeStatusAsync(id, request.Status, cancellationToken));
    }

    /// <summary>
    /// Assigns a request to a handler or releases it.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="request">The handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/assignee")]
    [RequirePermission(Permissions.Tickets.Manage)]
    public async Task<ActionResult> Assign(Guid id, TicketAssigneeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await tickets.AssignAsync(id, request.AssigneeId, cancellationToken));
    }

    /// <summary>
    /// Changes the category and the priority of a request.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="request">The category and the priority.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/classification")]
    [RequirePermission(Permissions.Tickets.Manage)]
    public async Task<ActionResult> Classify(Guid id, TicketClassificationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await tickets.ClassifyAsync(id, request.Category, request.Priority, cancellationToken));
    }

    /// <summary>
    /// Downloads a file of a request, or a photo in full size.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content of the file.</returns>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/octet-stream")]
    public Task<ActionResult> Attachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        => OpenAsync(id, attachmentId, thumbnail: false, cancellationToken);

    /// <summary>
    /// Gets the thumbnail of a photo.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="attachmentId">The photo.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The thumbnail.</returns>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/thumb")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "image/avif")]
    public Task<ActionResult> Thumbnail(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        => OpenAsync(id, attachmentId, thumbnail: true, cancellationToken);

    /// <summary>
    /// Opens the uploaded files, runs an action with them and closes them.
    /// </summary>
    /// <typeparam name="T">The result of the action.</typeparam>
    /// <param name="files">The uploaded files.</param>
    /// <param name="action">The action.</param>
    /// <returns>What the action returned.</returns>
    private static async Task<T> WithUploadsAsync<T>(List<IFormFile> files, Func<IReadOnlyList<FileUpload>, Task<T>> action)
    {
        var streams = new List<Stream>();
        try
        {
            var uploads = new List<FileUpload>();
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new FileUpload(stream, file.FileName, file.ContentType, file.Length));
            }

            return await action(uploads);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Streams a file: always as a download (<c>Content-Disposition: attachment</c>, <c>nosniff</c>), so no file is ever
    /// opened as a page of the site; photos are cached by the browser.
    /// </summary>
    /// <param name="id">The request.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="thumbnail">Whether the thumbnail is requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content of the file.</returns>
    private async Task<ActionResult> OpenAsync(Guid id, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken)
    {
        var result = await tickets.OpenAttachmentAsync(id, attachmentId, thumbnail, cancellationToken);
        if (result.IsSuccess && result.Value.ContentType == "image/avif")
        {
            Response.Headers.CacheControl = PhotoCacheControl;
        }

        return FromFile(result);
    }
}
