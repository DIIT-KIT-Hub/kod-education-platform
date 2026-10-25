namespace KOD.Web.Signing;

/// <summary>
/// The source of the trust list (section <c>Signing:TrustList</c>). Defaults to the files published by IIT, the vendor of the
/// signing library, which follow the register of qualified providers of the Central Certification Authority.
/// </summary>
public sealed class TrustListOptions
{
    /// <summary>
    /// Gets or sets the HTTPS address of the provider list (<c>CAs.json</c>).
    /// </summary>
    public Uri ProvidersUrl { get; set; } = new("https://iit.com.ua/download/productfiles/CAs.json");

    /// <summary>
    /// Gets or sets the HTTPS address of the CA certificate bundle (<c>CACertificates.p7b</c>).
    /// </summary>
    public Uri CertificatesUrl { get; set; } = new("https://iit.com.ua/download/productfiles/CACertificates.p7b");

    /// <summary>
    /// Gets or sets how often the trust list is refreshed; zero disables refreshing (the bundled copy is used).
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the folder (relative to the content root) where the last downloaded trust list is kept.
    /// </summary>
    public string CacheFolder { get; set; } = "App_Data/eusign";
}
