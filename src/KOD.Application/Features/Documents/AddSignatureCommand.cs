using KOD.Application.Common.Files;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Attaches a signature that was created in the signer's browser.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Format">The signature format.</param>
/// <param name="Packaging">The packaging.</param>
/// <param name="Level">The signature level reported by the library.</param>
/// <param name="KeyMedia">The key storage type.</param>
/// <param name="SignedAt">The signing time reported by the library.</param>
/// <param name="ReportedCertificate">The certificate data reported by the library (used when the server cannot parse the container).</param>
/// <param name="Signature">The signature file.</param>
public sealed record AddSignatureCommand(
    Guid DocumentId,
    SignatureFormat Format,
    SignaturePackaging Packaging,
    string? Level,
    KeyMediaType KeyMedia,
    DateTimeOffset? SignedAt,
    SignerCertificate ReportedCertificate,
    FileUpload Signature);
