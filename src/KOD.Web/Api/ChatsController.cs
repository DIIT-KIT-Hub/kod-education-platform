using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Features.Chats;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// One-to-one and group chats of the current user. New messages, edits, reactions and read positions also reach the
/// members' open pages through the SignalR hub.
/// </summary>
/// <param name="chats">The chat service.</param>
[Route("api/v1/chats")]
[RequirePermission(Permissions.Chats.Use)]
public sealed class ChatsController(IChatService chats) : ApiControllerBase
{
    /// <summary>
    /// The caching policy of photos: an attachment never changes under its identifier.
    /// </summary>
    private const string PhotoCacheControl = "private, max-age=31536000, immutable";

    /// <summary>
    /// Lists the chats of the current user, the most recently active first, with unread counts.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chats.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChatSummary>>> List(CancellationToken cancellationToken)
        => Ok(await chats.ListAsync(cancellationToken));

    /// <summary>
    /// Finds people to chat with by name or e-mail.
    /// </summary>
    /// <param name="term">The search text (at least two characters).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Up to 20 people.</returns>
    [HttpGet("people")]
    public async Task<ActionResult<IReadOnlyList<UserLookupItem>>> People([FromQuery] string? term, CancellationToken cancellationToken)
        => Ok(await chats.SearchPeopleAsync(term, cancellationToken));

    /// <summary>
    /// Opens the one-to-one chat with a person, creating it on the first call.
    /// </summary>
    /// <param name="request">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat.</returns>
    [HttpPost("direct")]
    public async Task<ActionResult<ChatIdResponse>> OpenDirect(OpenDirectChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await chats.OpenDirectAsync(request.UserId, cancellationToken);
        return result.IsSuccess ? Ok(new ChatIdResponse(result.Value)) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Creates a group chat.
    /// </summary>
    /// <param name="request">The title and the members.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the chat.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatIdResponse>> Create(CreateGroupChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await chats.CreateGroupAsync(request.Title, request.MemberIds, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, new ChatIdResponse(result.Value))
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Gets a chat with its members.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat with its members.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChatDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await chats.GetAsync(id, cancellationToken));

    /// <summary>
    /// Renames a group chat.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="request">The new title.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/title")]
    public async Task<ActionResult> Rename(Guid id, ChatTitleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await chats.RenameAsync(id, request.Title, cancellationToken));
    }

    /// <summary>
    /// Adds people to a group chat.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="request">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult> AddMembers(Guid id, ChatMembersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await chats.AddMembersAsync(id, request.UserIds, cancellationToken));
    }

    /// <summary>
    /// Leaves a group chat (the current user) or removes a person from it (the owner).
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
        => FromResult(await chats.RemoveMemberAsync(id, userId, cancellationToken));

    /// <summary>
    /// Clears the history of a chat for the current user only; a one-to-one chat also leaves their list until a new
    /// message arrives.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("{id:guid}/clear")]
    public async Task<ActionResult> ClearHistory(Guid id, CancellationToken cancellationToken)
        => FromResult(await chats.ClearHistoryAsync(id, cancellationToken));

    /// <summary>
    /// Deletes a chat for all its members with its messages and files (a one-to-one chat by either person, a group chat
    /// by its owner).
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteChat(Guid id, CancellationToken cancellationToken)
        => FromResult(await chats.DeleteChatAsync(id, cancellationToken));

    /// <summary>
    /// Gets messages: the newest page, the page before a number or the messages after a number.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="before">Return older messages than this number.</param>
    /// <param name="after">Return newer messages than this number.</param>
    /// <param name="limit">The number of messages (1–100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The messages, oldest first.</returns>
    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<ChatMessagePage>> Messages(
        Guid id,
        [FromQuery] long? before,
        [FromQuery] long? after,
        [FromQuery] int limit = IChatService.PageSize,
        CancellationToken cancellationToken = default)
        => FromResult(await chats.GetMessagesAsync(id, before, after, limit, cancellationToken));

    /// <summary>
    /// Sends a message (multipart: the HTML of the editor and up to ten files). A resend with the same client identifier
    /// returns the message already sent.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="form">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved message.</returns>
    [HttpPost("{id:guid}/messages")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxChatMessageSize + (1024 * 1024))]
    public async Task<ActionResult<ChatMessageDto>> Send(Guid id, [FromForm] SendChatMessageForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        var streams = new List<Stream>();
        try
        {
            var uploads = new List<FileUpload>();
            foreach (var file in form.Files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new FileUpload(stream, file.FileName, file.ContentType, file.Length));
            }

            return FromResult(await chats.SendAsync(id, new SendChatMessage(form.ClientId, form.Body, uploads), cancellationToken));
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
    /// Replaces the text of the current user's message.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="request">The new text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The edited message.</returns>
    [HttpPut("{id:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult<ChatMessageDto>> Edit(Guid id, Guid messageId, ChatMessageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await chats.EditAsync(id, messageId, request.Body, cancellationToken));
    }

    /// <summary>
    /// Deletes the current user's message for everyone with its files.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult> Delete(Guid id, Guid messageId, CancellationToken cancellationToken)
        => FromResult(await chats.DeleteAsync(id, messageId, cancellationToken));

    /// <summary>
    /// Deletes a message for the current user only (the others keep it).
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The empty placeholder the user sees now.</returns>
    [HttpPost("{id:guid}/messages/{messageId:guid}/hide")]
    public async Task<ActionResult<ChatMessageDto>> Hide(Guid id, Guid messageId, CancellationToken cancellationToken)
        => FromResult(await chats.HideAsync(id, messageId, cancellationToken));

    /// <summary>
    /// Sets the current user's reaction to a message; the same reaction again removes it.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message with its reactions.</returns>
    [HttpPost("{id:guid}/messages/{messageId:guid}/reactions/{kind}")]
    public async Task<ActionResult<ChatMessageDto>> React(Guid id, Guid messageId, ReactionKind kind, CancellationToken cancellationToken)
        => FromResult(await chats.ReactAsync(id, messageId, kind, cancellationToken));

    /// <summary>
    /// Marks the chat as read up to a message.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="request">The number of the last read message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, ChatReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await chats.MarkReadAsync(id, request.Sequence, cancellationToken));
    }

    /// <summary>
    /// Downloads a file of a message, or a photo in full size.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/octet-stream")]
    public Task<ActionResult> Attachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        => OpenAsync(id, attachmentId, thumbnail: false, cancellationToken);

    /// <summary>
    /// Gets the thumbnail of a photo.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The thumbnail.</returns>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/thumb")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "image/avif")]
    public Task<ActionResult> Thumbnail(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        => OpenAsync(id, attachmentId, thumbnail: true, cancellationToken);

    /// <summary>
    /// Streams an attachment: always as a download (<c>Content-Disposition: attachment</c>, <c>nosniff</c>), so no file
    /// is ever opened as a page of the site; photos are cached by the browser.
    /// </summary>
    /// <param name="id">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="thumbnail">Whether the thumbnail is requested.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    private async Task<ActionResult> OpenAsync(Guid id, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken)
    {
        var result = await chats.OpenAttachmentAsync(id, attachmentId, thumbnail, cancellationToken);
        if (result.IsSuccess && result.Value.ContentType == "image/avif")
        {
            Response.Headers.CacheControl = PhotoCacheControl;
        }

        return FromFile(result);
    }
}
