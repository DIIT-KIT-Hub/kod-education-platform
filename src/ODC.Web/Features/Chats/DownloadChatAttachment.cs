using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Streams a file of a message (or the thumbnail of a photo) to the members — always as a download.
/// </summary>
public static class DownloadChatAttachment
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="AttachmentId">The attachment.</param>
    /// <param name="Thumbnail">The thumbnail.</param>
    public sealed record Query(Guid ChatId, Guid AttachmentId, bool Thumbnail) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="files">The private file storage.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, IFileStorage files, ChatRoom room) : IRequestHandler<Query, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public async Task<Result<DownloadableFile>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var attachmentId = request.AttachmentId;
            var thumbnail = request.Thumbnail;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var attachment = await db.ChatAttachments
                .AsNoTracking()
                .Where(a => a.Id == attachmentId && a.ChatId == chatId && db.ChatMembers.Any(m => m.ChatId == chatId && m.UserId == me))
                .FirstOrDefaultAsync(cancellationToken);
            if (attachment is null)
            {
                return Error.NotFound(Texts.T("Файл не знайдено."));
            }

            var key = thumbnail && attachment.ThumbnailKey is { } small ? small : attachment.StorageKey;
            var name = attachment.Kind == ChatAttachmentKind.Image ? Path.ChangeExtension(attachment.FileName, ".avif") : attachment.FileName;
            return new DownloadableFile(await files.OpenReadAsync(key, cancellationToken), name, attachment.ContentType);
        }
    }

    /// <summary>
    /// The HTTP side of the slice.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var chats = ChatRoom.MapChats(api);
            chats.MapGet("{id:guid}/attachments/{attachmentId:guid}", AttachmentAsync).Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
            chats.MapGet("{id:guid}/attachments/{attachmentId:guid}/thumb", ThumbnailAsync).Produces(StatusCodes.Status200OK, contentType: "image/avif");
        }

        /// <summary>
        /// Downloads a file of a message, or a photo in full size.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="attachmentId">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response (cache headers of photos).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file stream.</returns>
        private static Task<Results<FileStreamHttpResult, ProblemHttpResult>> AttachmentAsync(Guid id, Guid attachmentId, IRequestHandler<Query, Result<DownloadableFile>> handler, HttpResponse response, CancellationToken cancellationToken)
            => OpenAsync(new Query(id, attachmentId, false), handler, response, cancellationToken);

        /// <summary>
        /// Gets the thumbnail of a photo.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="attachmentId">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response (cache headers of photos).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The thumbnail stream.</returns>
        private static Task<Results<FileStreamHttpResult, ProblemHttpResult>> ThumbnailAsync(Guid id, Guid attachmentId, IRequestHandler<Query, Result<DownloadableFile>> handler, HttpResponse response, CancellationToken cancellationToken)
            => OpenAsync(new Query(id, attachmentId, true), handler, response, cancellationToken);

        /// <summary>
        /// Opens the file and sets the cache policy of photos: an attachment never changes under its identifier.
        /// </summary>
        /// <param name="query">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file or the problem.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> OpenAsync(Query query, IRequestHandler<Query, Result<DownloadableFile>> handler, HttpResponse response, CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsSuccess && result.Value.ContentType == "image/avif")
            {
                response.Headers.CacheControl = "private, max-age=31536000, immutable";
            }

            return result.ToFile();
        }
    }
}
