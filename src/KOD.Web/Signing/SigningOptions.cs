namespace KOD.Web.Signing;

/// <summary>
/// Settings of browser-side signing (section <c>Signing</c>).
/// </summary>
public sealed class SigningOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Signing";

    /// <summary>
    /// Gets or sets the trust environment: <c>Production</c> (real QTSPs) or <c>Test</c> (test CA of the Central Certification Authority).
    /// </summary>
    public string Environment { get; set; } = "Production";

    /// <summary>
    /// Gets or sets the timeout of requests to CA servers.
    /// </summary>
    public int ProxyTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the CA hosts the proxy may contact (OCSP, TSP, CMP).
    /// </summary>
    public List<string> AllowedHosts { get; set; } = [];

    /// <summary>
    /// Gets or sets the ports the proxy may contact.
    /// </summary>
    public List<int> AllowedPorts { get; set; } = [80, 443];

    /// <summary>
    /// Gets a value indicating whether the test environment is used.
    /// </summary>
    public bool IsTest => string.Equals(Environment, "Test", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the URL of the QTSP list for the library.
    /// </summary>
    public string CasUrl => IsTest ? "/lib/eusign/data/CAs.Test.json" : "/lib/eusign/data/CAs.json";

    /// <summary>
    /// Gets the URL of the CA certificates bundle for the library.
    /// </summary>
    public string CertificatesUrl => IsTest ? "/lib/eusign/data/CACertificates.Test.p7b" : "/lib/eusign/data/CACertificates.p7b";
}
