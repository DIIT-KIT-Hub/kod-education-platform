using System.Buffers;
using Microsoft.Extensions.Options;

namespace KOD.Web.Signing;

/// <summary>
/// Keeps the trust list current: downloads the provider list and the CA certificate bundle from the official source at
/// start-up and then every <see cref="TrustListOptions.RefreshInterval"/>. A download replaces the current list only when
/// both files are valid; otherwise the previous list stays in use.
/// </summary>
/// <param name="store">The trust list store.</param>
/// <param name="clientFactory">The HTTP client factory.</param>
/// <param name="options">The signing options.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class TrustListRefreshService(
    TrustListStore store,
    IHttpClientFactory clientFactory,
    IOptions<SigningOptions> options,
    TimeProvider timeProvider,
    ILogger<TrustListRefreshService> logger) : BackgroundService
{
    /// <summary>
    /// The named HTTP client.
    /// </summary>
    public const string HttpClientName = "eusign-trust-list";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value.TrustList;
        if (settings.RefreshInterval <= TimeSpan.Zero)
        {
            return;
        }

        // Load the current copy first so the signing pages never wait for the network.
        _ = store.Current;

        using var timer = new PeriodicTimer(settings.RefreshInterval, timeProvider);
        do
        {
            try
            {
                await RefreshAsync(settings, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                LogRefreshFailed(logger, exception.Message, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Downloads a file over HTTPS with a size limit.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="address">The address.</param>
    /// <param name="maxBytes">The size limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content.</returns>
    private static async Task<byte[]> DownloadAsync(HttpClient client, Uri address, int maxBytes, CancellationToken cancellationToken)
    {
        if (address.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"The trust list must be downloaded over HTTPS: {address}");
        }

        using var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw new InvalidOperationException($"{address} is larger than {maxBytes} bytes.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > maxBytes)
                {
                    throw new InvalidOperationException($"{address} is larger than {maxBytes} bytes.");
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
    /// Logs a successful update.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="providers">The number of providers.</param>
    /// <param name="certificates">The number of certificates.</param>
    /// <param name="previousProviders">The previous number of providers.</param>
    /// <param name="previousCertificates">The previous number of certificates.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Trust list updated: {Providers} providers, {Certificates} CA certificates (was {PreviousProviders} and {PreviousCertificates})")]
    private static partial void LogUpdated(ILogger logger, int providers, int certificates, int previousProviders, int previousCertificates);

    /// <summary>
    /// Logs an unchanged list.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Trust list is up to date")]
    private static partial void LogUnchanged(ILogger logger);

    /// <summary>
    /// Logs rejected files.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">The reason.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Downloaded trust list rejected, keeping the current one: {Reason}")]
    private static partial void LogRejected(ILogger logger, string reason);

    /// <summary>
    /// Logs a failed refresh.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Trust list refresh failed, keeping the current one: {Reason}")]
    private static partial void LogRefreshFailed(ILogger logger, string reason, Exception exception);

    /// <summary>
    /// Downloads, validates and stores the trust list once.
    /// </summary>
    /// <param name="settings">The source settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when done.</returns>
    private async Task RefreshAsync(TrustListOptions settings, CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient(HttpClientName);
        var providers = await DownloadAsync(client, settings.ProvidersUrl, TrustList.MaxProvidersBytes, cancellationToken);
        var certificates = await DownloadAsync(client, settings.CertificatesUrl, TrustList.MaxCertificatesBytes, cancellationToken);

        if (!TrustList.TryCreate(providers, certificates, timeProvider.GetUtcNow(), out var list, out var error))
        {
            LogRejected(logger, error);
            return;
        }

        var current = store.Current;
        if (list.ProvidersETag == current.ProvidersETag && list.CertificatesETag == current.CertificatesETag)
        {
            LogUnchanged(logger);
            return;
        }

        await store.ReplaceAsync(list, cancellationToken);
        LogUpdated(logger, list.ProviderCount, list.CertificateCount, current.ProviderCount, current.CertificateCount);
    }
}
