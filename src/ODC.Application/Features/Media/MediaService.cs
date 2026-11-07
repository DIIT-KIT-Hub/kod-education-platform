using ODC.Application.Abstractions;
using ODC.Application.Common.Files;
using ODC.Application.Common.Media;
using ODC.Application.Common.Results;
using ODC.Domain.Access;
using ODC.Domain.Media;

namespace ODC.Application.Features.Media;

/// <summary>
/// Default <see cref="IMediaService"/>.
/// </summary>
/// <param name="currentUser">The current user.</param>
/// <param name="library">The image library.</param>
internal sealed class MediaService(ICurrentUser currentUser, MediaLibrary library) : IMediaService
{
    /// <inheritdoc />
    public async Task<Result<MediaDto>> UploadNewsImageAsync(FileUpload file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (currentUser.Id is not { } me || !currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var added = await library.AddAsync(file, MediaPurpose.News, me, crop: null, cancellationToken);
        if (!added.IsSuccess)
        {
            return added.Error;
        }

        await library.SaveChangesAsync(cancellationToken);
        var media = added.Value;
        return new MediaDto(media.Id, MediaUrls.Of(media.Id), MediaUrls.ThumbnailOf(media.Id), media.Width, media.Height, media.Size);
    }
}
