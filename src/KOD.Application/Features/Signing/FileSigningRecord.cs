using KOD.Domain.Documents;

namespace KOD.Application.Features.Signing;

/// <summary>
/// What the file signing tool reports after signing files in the browser: names and parameters only, never the content.
/// </summary>
/// <param name="FileNames">The names of the signed files.</param>
/// <param name="Format">The signature format.</param>
/// <param name="Level">The signature level (e.g. <c>B-LT</c>).</param>
/// <param name="SignerName">The signer's full name from the certificate.</param>
/// <param name="CertificateSerial">The certificate serial number.</param>
/// <param name="Issuer">The certificate issuer (trust service provider).</param>
public sealed record FileSigningRecord(
    IReadOnlyList<string> FileNames,
    SignatureFormat Format,
    string? Level,
    string? SignerName,
    string? CertificateSerial,
    string? Issuer);
