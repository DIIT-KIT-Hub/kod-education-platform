using KOD.Application.Common.Files;
using KOD.Application.Features.Media;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Uploads images for news articles (the editor inserts the returned URLs). Images are served by <c>GET /media/{id}</c>.
/// </summary>
/// <param name="media">The media service.</param>
[Route("api/v1/media")]
[RequirePermission(Permissions.News.Manage)]
public sealed class MediaController(IMediaService media) : ApiControllerBase
{
    /// <summary>
    /// Optimizes and stores an image (JPEG, PNG, WebP, AVIF or GIF up to 15 MB): AVIF, at most 1600 px and 100 KB, plus a thumbnail.
    /// </summary>
    /// <param name="file">The image.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored image.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxImageSize + (64 * 1024))]
    public async Task<ActionResult<MediaDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        return FromResult(await media.UploadNewsImageAsync(new FileUpload(content, file.FileName, file.ContentType, file.Length), cancellationToken));
    }
}
