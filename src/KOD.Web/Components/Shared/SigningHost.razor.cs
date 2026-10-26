using KOD.Web.Signing;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// Hosts the browser-side signing UI (official IIT EUSignCP-JS library in a Web Worker).
/// Modes: <c>document</c> (sign a stored document and upload the signature), <c>sign</c> (sign local files and download),
/// <c>verify</c> (verify a signature locally).
/// </summary>
public partial class SigningHost
{
    /// <summary>
    /// The relay URL for CA requests.
    /// </summary>
    private const string ProxyUrl = EuSignProxyEndpoint.Route;

    /// <summary>
    /// The URL of the current provider list.
    /// </summary>
    private const string ProvidersUrl = TrustListEndpoints.ProvidersRoute;

    /// <summary>
    /// The URL of the current CA certificate bundle.
    /// </summary>
    private const string CertificatesUrl = TrustListEndpoints.CertificatesRoute;

    /// <summary>
    /// Gets or sets the mode: <c>document</c>, <c>sign</c> or <c>verify</c>.
    /// </summary>
    [Parameter]
    public string Mode { get; set; } = "sign";

    /// <summary>
    /// Gets or sets the document identifier (document mode).
    /// </summary>
    [Parameter]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the document title (document mode).
    /// </summary>
    [Parameter]
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Gets or sets the document file name (document mode).
    /// </summary>
    [Parameter]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the expected SHA-256 of the document file; the browser checks it before signing.
    /// </summary>
    [Parameter]
    public string? FileSha256 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document is a PDF (PAdES available).
    /// </summary>
    [Parameter]
    public bool IsPdf { get; set; }

    /// <summary>
    /// Gets the URL of the document file.
    /// </summary>
    private string? FileUrl => DocumentId is { } id ? $"/api/v1/documents/{id}/file" : null;

    /// <summary>
    /// Gets the URL the signature is uploaded to.
    /// </summary>
    private string? UploadUrl => DocumentId is { } id ? $"/api/v1/documents/{id}/signatures" : null;

    /// <summary>
    /// Gets the URL of the signing session (the turn to sign and the version to add the signature to).
    /// </summary>
    private string? SessionUrl => DocumentId is { } id ? $"/api/v1/documents/{id}/signing-session" : null;
}
