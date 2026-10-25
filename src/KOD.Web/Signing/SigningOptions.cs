namespace KOD.Web.Signing;

/// <summary>
/// Settings of browser-side signing (section <c>Signing</c>). Only real qualified trust service providers are supported:
/// there is no test environment.
/// </summary>
public sealed class SigningOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Signing";

    /// <summary>
    /// Gets or sets the timeout of requests to CA servers.
    /// </summary>
    public int ProxyTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets additional CA hosts the proxy may contact (OCSP, TSP, CMP) besides the hosts of the current trust list,
    /// e.g. OCSP / TSP addresses taken from certificate extensions.
    /// </summary>
    public List<string> AllowedHosts { get; set; } = [];

    /// <summary>
    /// Gets or sets additional ports the proxy may contact besides the ports of the current trust list.
    /// </summary>
    public List<int> AllowedPorts { get; set; } = [80, 443];

    /// <summary>
    /// Gets or sets where the trust list (providers and CA certificates) is refreshed from.
    /// </summary>
    public TrustListOptions TrustList { get; set; } = new();
}
