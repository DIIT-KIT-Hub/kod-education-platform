using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace KOD.Web.Signing;

/// <summary>
/// The HTTP relay required by the IIT EUSignCP-JS library to reach OCSP, TSP and CMP servers of qualified trust service
/// providers (browsers cannot call them directly because of CORS). Only public protocol messages pass through it:
/// certificate status requests, time-stamp requests (a hash) and certificate lookups. Private keys and passwords never do.
/// </summary>
/// <remarks>
/// Contract (defined by the library): <c>GET|POST {path}?address={absolute CA url}&amp;contentType={type}</c>;
/// POST bodies are Base64 text with <c>Content-Type: X-user/base64-data</c>; responses are Base64 text with status 200.
/// </remarks>
internal static partial class EuSignProxyEndpoint
{
    /// <summary>
    /// The route of the relay.
    /// </summary>
    public const string Route = "/api/v1/eusign/proxy";

    /// <summary>
    /// The named HTTP client.
    /// </summary>
    public const string HttpClientName = "eusign";

    /// <summary>
    /// The rate limiting policy.
    /// </summary>
    public const string RateLimitPolicy = "eusign-proxy";

    /// <summary>
    /// The content type of Base64 payloads.
    /// </summary>
    private const string Base64ContentType = "X-user/base64-data";

    /// <summary>
    /// Maximum Base64 request body length.
    /// </summary>
    private const int MaxRequestLength = 10_000_000;

    /// <summary>
    /// Maximum upstream response size.
    /// </summary>
    private const int MaxResponseSize = 20_000_000;

    /// <summary>
    /// Maximum length of the <c>address</c> parameter.
    /// </summary>
    private const int MaxAddressLength = 255;

    /// <summary>
    /// Content types accepted by CA servers.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes =
        new([string.Empty, "application/timestamp-query", "application/ocsp-request", "application/json"], StringComparer.OrdinalIgnoreCase);

    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the relay endpoint (authenticated users only, rate limited).
        /// </summary>
        /// <returns>The endpoint convention builder.</returns>
        public RouteHandlerBuilder MapEuSignProxy()
            => endpoints.MapMethods(Route, [HttpMethods.Get, HttpMethods.Post], HandleAsync)
                .RequireAuthorization()
                .RequireRateLimiting(RateLimitPolicy)
                .DisableAntiforgery()
                .ExcludeFromDescription();
    }

    /// <summary>
    /// Relays one request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="clientFactory">The HTTP client factory.</param>
    /// <param name="options">The signing options.</param>
    /// <param name="trustList">The current trust list (servers of the listed providers are allowed).</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The relay result.</returns>
    private static async Task<IResult> HandleAsync(
        HttpContext context,
        IHttpClientFactory clientFactory,
        IOptions<SigningOptions> options,
        TrustListStore trustList,
        ILogger<SigningOptions> logger)
    {
        var cancellationToken = context.RequestAborted;
        var address = context.Request.Query["address"].ToString();
        if (address.Length is 0 or > MaxAddressLength)
        {
            return TypedResults.BadRequest();
        }

        if (!address.Contains("://", StringComparison.Ordinal))
        {
            address = "http://" + address;
        }

        // Destinations: servers of the providers in the current trust list plus the configured extra hosts.
        var settings = options.Value;
        var list = trustList.Current;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !(list.ContainsHost(uri.IdnHost) || settings.AllowedHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))
            || !(list.Ports.Contains(uri.Port) || settings.AllowedPorts.Contains(uri.Port)))
        {
            LogRejected(logger, uri?.Host ?? "invalid");
            return TypedResults.BadRequest();
        }

        var contentType = context.Request.Query.TryGetValue("contentType", out var requested)
            ? requested.ToString()
            : InferContentType(uri.AbsolutePath);
        if (!AllowedContentTypes.Contains(contentType))
        {
            return TypedResults.BadRequest();
        }

        using var request = new HttpRequestMessage(HttpMethods.IsPost(context.Request.Method) ? HttpMethod.Post : HttpMethod.Get, uri);
        if (request.Method == HttpMethod.Post)
        {
            if (context.Request.ContentType?.Contains(Base64ContentType, StringComparison.OrdinalIgnoreCase) != true)
            {
                return TypedResults.BadRequest();
            }

            var text = await ReadBoundedTextAsync(context.Request.Body, MaxRequestLength, cancellationToken);
            if (text is null)
            {
                return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            byte[] body;
            try
            {
                body = Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                return TypedResults.BadRequest();
            }

            request.Content = new ByteArrayContent(body);
            if (contentType.Length > 0)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
        }

        try
        {
            using var response = await clientFactory.CreateClient(HttpClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogUpstreamFailed(logger, uri.Host, (int)response.StatusCode);
                return TypedResults.StatusCode(StatusCodes.Status502BadGateway);
            }

            var data = await ReadBoundedBytesAsync(await response.Content.ReadAsStreamAsync(cancellationToken), MaxResponseSize, cancellationToken);
            return data is null
                ? TypedResults.StatusCode(StatusCodes.Status502BadGateway)
                : TypedResults.Text(Convert.ToBase64String(data), Base64ContentType, Encoding.ASCII);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUpstreamUnavailable(logger, uri.Host, exception);
            return TypedResults.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
    }

    /// <summary>
    /// Infers the content type from well-known OCSP / TSP paths.
    /// </summary>
    /// <param name="path">The URL path.</param>
    /// <returns>The content type.</returns>
    private static string InferContentType(string path) => path.TrimEnd('/').ToLowerInvariant() switch
    {
        "/services/ocsp" or "/public/ocsp" or "/ocsp" or "/ocspsrv/ocsp" => "application/ocsp-request",
        "/services/tsp" or "/services/tsp/rsa" or "/services/tsp/ecdsa" or "/public/tsa" or "/public/tsp" or "/tsp" or "/tsphttpserver/tsp"
            => "application/timestamp-query",
        _ => string.Empty,
    };

    /// <summary>
    /// Reads at most <paramref name="maxLength"/> bytes of ASCII text.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="maxLength">The limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The text, or <see langword="null"/> when the limit is exceeded.</returns>
    private static async Task<string?> ReadBoundedTextAsync(Stream stream, int maxLength, CancellationToken cancellationToken)
    {
        var bytes = await ReadBoundedBytesAsync(stream, maxLength, cancellationToken);
        return bytes is null ? null : Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Reads at most <paramref name="maxLength"/> bytes.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="maxLength">The limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bytes, or <see langword="null"/> when the limit is exceeded.</returns>
    private static async Task<byte[]?> ReadBoundedBytesAsync(Stream stream, int maxLength, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > maxLength)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Logs a rejected destination.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="host">The requested host.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Signing proxy rejected the destination host {Host}")]
    private static partial void LogRejected(ILogger logger, string host);

    /// <summary>
    /// Logs an upstream error status.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="host">The CA host.</param>
    /// <param name="status">The status code.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "CA server {Host} answered {Status}")]
    private static partial void LogUpstreamFailed(ILogger logger, string host, int status);

    /// <summary>
    /// Logs an unreachable upstream.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="host">The CA host.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "CA server {Host} is unavailable")]
    private static partial void LogUpstreamUnavailable(ILogger logger, string host, Exception exception);
}
