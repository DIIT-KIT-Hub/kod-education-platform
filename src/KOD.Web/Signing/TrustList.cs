using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text.Json;

namespace KOD.Web.Signing;

/// <summary>
/// A validated snapshot of the trust list used by the signing library: the qualified trust service providers
/// (<c>CAs.json</c>) and their CA certificates (<c>CACertificates.p7b</c>). Immutable; replaced as a whole on refresh.
/// </summary>
internal sealed class TrustList
{
    /// <summary>
    /// The largest accepted provider list.
    /// </summary>
    public const int MaxProvidersBytes = 2 * 1024 * 1024;

    /// <summary>
    /// The largest accepted certificate bundle.
    /// </summary>
    public const int MaxCertificatesBytes = 32 * 1024 * 1024;

    /// <summary>
    /// The smallest plausible number of providers and certificates (protects against truncated or substituted files).
    /// </summary>
    private const int MinimumEntries = 10;

    /// <summary>
    /// Fields of a provider entry that hold CA server addresses.
    /// </summary>
    private static readonly string[] AddressFields = ["address", "ocspAccessPointAddress", "cmpAddress", "tspAddress"];

    /// <summary>
    /// Fields of a provider entry that hold CA server ports.
    /// </summary>
    private static readonly string[] PortFields = ["ocspAccessPointPort", "cmpPort", "tspAddressPort", "addressPort"];

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustList"/> class.
    /// </summary>
    /// <param name="providers">The provider list.</param>
    /// <param name="certificates">The certificate bundle.</param>
    /// <param name="providerCount">The number of providers.</param>
    /// <param name="certificateCount">The number of certificates.</param>
    /// <param name="hosts">The CA server hosts.</param>
    /// <param name="ports">The CA server ports.</param>
    /// <param name="updatedAt">When the snapshot was obtained.</param>
    private TrustList(byte[] providers, byte[] certificates, int providerCount, int certificateCount, FrozenSet<string> hosts, FrozenSet<int> ports, DateTimeOffset updatedAt)
    {
        Providers = providers;
        Certificates = certificates;
        ProviderCount = providerCount;
        CertificateCount = certificateCount;
        Hosts = hosts;
        Ports = ports;
        UpdatedAt = updatedAt;
        ProvidersETag = ETag(providers);
        CertificatesETag = ETag(certificates);
    }

    /// <summary>
    /// Gets the provider list (<c>CAs.json</c>) as published.
    /// </summary>
    public byte[] Providers { get; }

    /// <summary>
    /// Gets the CA certificate bundle (<c>CACertificates.p7b</c>) as published.
    /// </summary>
    public byte[] Certificates { get; }

    /// <summary>
    /// Gets the number of providers.
    /// </summary>
    public int ProviderCount { get; }

    /// <summary>
    /// Gets the number of CA certificates.
    /// </summary>
    public int CertificateCount { get; }

    /// <summary>
    /// Gets the hosts of the providers' OCSP, TSP and CMP servers (lower case).
    /// </summary>
    public FrozenSet<string> Hosts { get; }

    /// <summary>
    /// Gets the ports of the providers' servers.
    /// </summary>
    public FrozenSet<int> Ports { get; }

    /// <summary>
    /// Gets when the snapshot was obtained.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>
    /// Gets the entity tag of the provider list.
    /// </summary>
    public string ProvidersETag { get; }

    /// <summary>
    /// Gets the entity tag of the certificate bundle.
    /// </summary>
    public string CertificatesETag { get; }

    /// <summary>
    /// Validates downloaded or bundled files and creates a snapshot.
    /// </summary>
    /// <param name="providers">The provider list.</param>
    /// <param name="certificates">The certificate bundle.</param>
    /// <param name="updatedAt">When the files were obtained.</param>
    /// <param name="list">The snapshot.</param>
    /// <param name="error">Why the files were rejected.</param>
    /// <returns><see langword="true"/> when the files are valid.</returns>
    public static bool TryCreate(byte[] providers, byte[] certificates, DateTimeOffset updatedAt, [NotNullWhen(true)] out TrustList? list, [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(certificates);
        list = null;

        if (providers.Length is 0 or > MaxProvidersBytes || certificates.Length is 0 or > MaxCertificatesBytes)
        {
            error = "Unexpected file size.";
            return false;
        }

        HashSet<string> hosts = new(StringComparer.Ordinal);
        HashSet<int> ports = [];
        int providerCount;
        try
        {
            using var document = JsonDocument.Parse(providers);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The provider list is not a JSON array.";
                return false;
            }

            providerCount = 0;
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty("issuerCNs", out var names)
                    || names.ValueKind != JsonValueKind.Array
                    || names.GetArrayLength() == 0
                    || names.EnumerateArray().Any(static n => n.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(n.GetString())))
                {
                    error = "A provider entry has no issuer names.";
                    return false;
                }

                foreach (var field in AddressFields)
                {
                    if (entry.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String && HostOf(value.GetString()) is { } host)
                    {
                        hosts.Add(host);
                    }
                }

                foreach (var field in PortFields)
                {
                    if (entry.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
                        && int.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is > 0 and <= 65535)
                    {
                        ports.Add(port);
                    }
                }

                providerCount++;
            }
        }
        catch (JsonException)
        {
            error = "The provider list is not valid JSON.";
            return false;
        }

        int certificateCount;
        try
        {
            var bundle = new SignedCms();
            bundle.Decode(certificates);
            certificateCount = bundle.Certificates.Count;
        }
        catch (CryptographicException)
        {
            error = "The certificate bundle is not a valid PKCS#7 file.";
            return false;
        }

        if (providerCount < MinimumEntries || certificateCount < MinimumEntries)
        {
            error = $"Too few entries ({providerCount} providers, {certificateCount} certificates).";
            return false;
        }

        list = new TrustList(providers, certificates, providerCount, certificateCount, hosts.ToFrozenSet(StringComparer.Ordinal), ports.ToFrozenSet(), updatedAt);
        error = null;
        return true;
    }

    /// <summary>
    /// Whether the proxy may contact a host: a server of a listed provider.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The result.</returns>
    public bool ContainsHost(string host) => Hosts.Contains(host.ToLowerInvariant());

    /// <summary>
    /// Extracts the host from an address such as <c>http://ca.example.ua/services/ocsp/</c> or <c>ca.example.ua</c>.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>The lower-case host, or <see langword="null"/>.</returns>
    internal static string? HostOf(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var value = address.Trim();
        var scheme = value.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0)
        {
            value = value[(scheme + 3)..];
        }

        var end = value.IndexOfAny(['/', ':', '?', '#']);
        var host = (end >= 0 ? value[..end] : value).ToLowerInvariant();
        return Uri.CheckHostName(host) is UriHostNameType.Dns or UriHostNameType.IPv4 ? host : null;
    }

    /// <summary>
    /// Computes a strong entity tag of the content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The quoted entity tag.</returns>
    private static string ETag(byte[] content) => $"\"{Convert.ToHexStringLower(SHA256.HashData(content))}\"";
}
