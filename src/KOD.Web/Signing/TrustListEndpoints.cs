using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace KOD.Web.Signing;

/// <summary>
/// Serves the current trust list to the signing library: <c>GET /api/v1/eusign/providers</c> (<c>CAs.json</c>) and
/// <c>GET /api/v1/eusign/certificates</c> (<c>CACertificates.p7b</c>). Responses carry an entity tag, so browsers revalidate
/// and download the 1.5 MB bundle again only after it has changed.
/// </summary>
internal static class TrustListEndpoints
{
    /// <summary>
    /// The route of the provider list.
    /// </summary>
    public const string ProvidersRoute = "/api/v1/eusign/providers";

    /// <summary>
    /// The route of the CA certificate bundle.
    /// </summary>
    public const string CertificatesRoute = "/api/v1/eusign/certificates";

    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the trust list endpoints (authenticated users only).
        /// </summary>
        /// <returns>The endpoint route builder.</returns>
        public IEndpointRouteBuilder MapTrustList()
        {
            endpoints.MapGet(ProvidersRoute, static (HttpContext context, TrustListStore store) =>
                {
                    var list = store.Current;
                    return Serve(context, list.Providers, "application/json", list.ProvidersETag, list.UpdatedAt);
                })
                .RequireAuthorization()
                .ExcludeFromDescription();

            endpoints.MapGet(CertificatesRoute, static (HttpContext context, TrustListStore store) =>
                {
                    var list = store.Current;
                    return Serve(context, list.Certificates, "application/x-pkcs7-certificates", list.CertificatesETag, list.UpdatedAt);
                })
                .RequireAuthorization()
                .ExcludeFromDescription();

            return endpoints;
        }
    }

    /// <summary>
    /// Returns the content with validators; conditional requests get <c>304 Not Modified</c>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="content">The content.</param>
    /// <param name="contentType">The media type.</param>
    /// <param name="etag">The entity tag.</param>
    /// <param name="lastModified">The modification time.</param>
    /// <returns>The result.</returns>
    private static FileContentHttpResult Serve(HttpContext context, byte[] content, string contentType, string etag, DateTimeOffset lastModified)
    {
        context.Response.Headers.CacheControl = "private, no-cache";
        return TypedResults.Bytes(content, contentType, lastModified: lastModified, entityTag: new EntityTagHeaderValue(etag));
    }
}
