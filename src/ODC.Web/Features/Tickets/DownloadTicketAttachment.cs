using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Streams a file of a request (or the thumbnail of a photo) to its author or a handler — always as a download
/// (<c>Content-Disposition: attachment</c>, <c>nosniff</c>), so no file is ever opened as a page of the site.
/// </summary>
public static class DownloadTicketAttachment
{
    /// <summary>
    /// The caching policy of photos: an attachment never changes under its identifier.
    /// </summary>
    private const string PhotoCacheControl = "private, max-age=31536000, immutable";

    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="TicketId">The ticket.</param>
    /// <param name="AttachmentId">The file.</param>
    /// <param name="Thumbnail">Whether the thumbnail of a photo is wanted.</param>
    public sealed record Query(Guid TicketId, Guid AttachmentId, bool Thumbnail) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// Opens the file when the user may see the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="files">The private file storage.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, TicketDesk desk, IFileStorage files) : IRequestHandler<Query, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public async Task<Result<DownloadableFile>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return TicketDesk.NotFound();
            }

            var (id, attachmentId) = (request.TicketId, request.AttachmentId);
            var handler = desk.IsHandler;
            var attachment = await db.TicketAttachments
                .AsNoTracking()
                .Where(a => a.Id == attachmentId && a.TicketId == id && db.Tickets.Any(t => t.Id == id && (handler || t.AuthorId == me)))
                .FirstOrDefaultAsync(cancellationToken);
            if (attachment is null)
            {
                return Error.NotFound(Texts.T("Файл не знайдено."));
            }

            var key = request.Thumbnail && attachment.ThumbnailKey is { } small ? small : attachment.StorageKey;
            var name = attachment.ThumbnailKey is null ? attachment.FileName : Path.ChangeExtension(attachment.FileName, ".avif");
            return new DownloadableFile(await files.OpenReadAsync(key, cancellationToken), name, attachment.ContentType);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/tickets/{id}/attachments/{attachmentId}</c> and <c>…/thumb</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var tickets = TicketDesk.MapTickets(api);
            tickets.MapGet("{id:guid}/attachments/{attachmentId:guid}", AttachmentAsync).Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
            tickets.MapGet("{id:guid}/attachments/{attachmentId:guid}/thumb", ThumbnailAsync).Produces(StatusCodes.Status200OK, contentType: "image/avif");
        }

        /// <summary>
        /// Downloads a file of a request, or a photo in full size.
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="attachmentId">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response (cache headers of photos).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file stream.</returns>
        private static Task<Results<FileStreamHttpResult, ProblemHttpResult>> AttachmentAsync(
            Guid id,
            Guid attachmentId,
            IRequestHandler<Query, Result<DownloadableFile>> handler,
            HttpResponse response,
            CancellationToken cancellationToken)
            => OpenAsync(new Query(id, attachmentId, Thumbnail: false), handler, response, cancellationToken);

        /// <summary>
        /// Gets the thumbnail of a photo.
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="attachmentId">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response (cache headers of photos).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The thumbnail.</returns>
        private static Task<Results<FileStreamHttpResult, ProblemHttpResult>> ThumbnailAsync(
            Guid id,
            Guid attachmentId,
            IRequestHandler<Query, Result<DownloadableFile>> handler,
            HttpResponse response,
            CancellationToken cancellationToken)
            => OpenAsync(new Query(id, attachmentId, Thumbnail: true), handler, response, cancellationToken);

        /// <summary>
        /// Opens the file and sets the cache policy of photos.
        /// </summary>
        /// <param name="query">The file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file or the problem.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> OpenAsync(
            Query query,
            IRequestHandler<Query, Result<DownloadableFile>> handler,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsSuccess && result.Value.ContentType == "image/avif")
            {
                response.Headers.CacheControl = PhotoCacheControl;
            }

            return result.ToFile();
        }
    }
}
