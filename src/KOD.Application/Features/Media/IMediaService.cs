using KOD.Application.Common.Files;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Media;

/// <summary>
/// Uploads images for news articles (the rich text editor). Profile photos are uploaded through the profile.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Optimizes an image (AVIF, 10–100 KB) and stores it; it becomes part of an article when the article is saved,
    /// otherwise it is removed after a day.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored image.</returns>
    Task<Result<MediaDto>> UploadNewsImageAsync(FileUpload file, CancellationToken cancellationToken);
}
