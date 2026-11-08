using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Media;

namespace ODC.Web.Features.Media;

/// <summary>
/// Uploads an image for news articles (the rich text editor inserts the returned URLs). The image becomes part of an
/// article when the article is saved, otherwise it is removed after a day. Profile photos go through the profile.
/// </summary>
public static class UploadNewsImage
{
    /// <summary>
    /// The upload.
    /// </summary>
    /// <param name="File">The uploaded file.</param>
    [RequirePermission(Permissions.News.Manage)]
    public sealed record Command(FileUpload File) : ICommand<Result<MediaDto>>;

    /// <summary>
    /// Optimizes the image (AVIF, 10–100 KB) and stores it.
    /// </summary>
    /// <param name="currentUser">The current user.</param>
    /// <param name="library">The image library.</param>
    internal sealed class Handler(ICurrentUser currentUser, MediaLibrary library) : IRequestHandler<Command, Result<MediaDto>>
    {
        /// <inheritdoc />
        public async Task<Result<MediaDto>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Forbidden();
            }

            var added = await library.AddAsync(request.File, MediaPurpose.News, me, crop: null, cancellationToken);
            if (!added.IsSuccess)
            {
                return added.Error;
            }

            await library.SaveChangesAsync(cancellationToken);
            var media = added.Value;
            return new MediaDto(media.Id, MediaUrls.Of(media.Id), MediaUrls.ThumbnailOf(media.Id), media.Width, media.Height, media.Size);
        }
    }

    /// <summary>
    /// <c>POST /api/v1/media</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => api.MapPost("media", HandleAsync)
                .RequirePermission(Permissions.News.Manage)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxImageSize + (64 * 1024)))
                .WithTags("Media");

        /// <summary>
        /// Optimizes and stores an image (JPEG, PNG, WebP, AVIF or GIF up to 15 MB): AVIF, at most 1600 px and 100 KB,
        /// plus a thumbnail.
        /// </summary>
        /// <param name="file">The image.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The stored image.</returns>
        private static async Task<Results<Ok<MediaDto>, ProblemHttpResult>> HandleAsync(
            IFormFile file,
            IRequestHandler<Command, Result<MediaDto>> handler,
            CancellationToken cancellationToken)
        {
            await using var content = file.OpenReadStream();
            return (await handler.HandleAsync(new Command(new FileUpload(content, file.FileName, file.ContentType, file.Length)), cancellationToken)).ToOk();
        }
    }
}
