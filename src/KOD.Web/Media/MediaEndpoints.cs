using KOD.Application.Abstractions;
using Microsoft.Net.Http.Headers;

namespace KOD.Web.Media;

/// <summary>
/// Serves uploaded images to signed-in users. An image never changes under its identifier, so browsers keep it for a
/// year without asking again, and a repeated request is answered with 304 by its entity tag.
/// </summary>
internal static class MediaEndpoints
{
    /// <summary>
    /// The caching policy of images (private: they are not public content).
    /// </summary>
    private const string CacheControl = "private, max-age=31536000, immutable";

    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>GET /media/{id}</c> (the main rendition) and <c>GET /media/{id}/thumb</c>.
        /// </summary>
        /// <returns>The endpoint convention builder.</returns>
        public IEndpointConventionBuilder MapMediaEndpoints()
        {
            var group = endpoints.MapGroup("/media");
            group.MapGet("/{id:guid}", static (Guid id, HttpContext context, IMediaStorage storage) => Serve(id, thumbnail: false, context, storage));
            group.MapGet("/{id:guid}/thumb", static (Guid id, HttpContext context, IMediaStorage storage) => Serve(id, thumbnail: true, context, storage));
            return group;
        }
    }

    /// <summary>
    /// Streams a rendition.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="thumbnail">Whether the thumbnail is requested.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="storage">The image storage.</param>
    /// <returns>The image, 304 or 404.</returns>
    private static IResult Serve(Guid id, bool thumbnail, HttpContext context, IMediaStorage storage)
    {
        var stream = storage.OpenRead(id, thumbnail);
        if (stream is null)
        {
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = CacheControl;
        var tag = new EntityTagHeaderValue($"\"{id:N}{(thumbnail ? "t" : "m")}\"");
        return Results.Stream(stream, "image/avif", entityTag: tag);
    }
}
