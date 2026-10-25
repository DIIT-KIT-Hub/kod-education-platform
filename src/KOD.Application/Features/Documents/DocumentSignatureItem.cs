using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// A collected signature.
/// </summary>
/// <param name="Id">The signature identifier.</param>
/// <param name="UserId">The signer's user identifier.</param>
/// <param name="UserName">The signer's full name in the cabinet.</param>
/// <param name="Format">The signature format.</param>
/// <param name="Packaging">The packaging.</param>
/// <param name="Level">The signature level.</param>
/// <param name="KeyMedia">The key storage type.</param>
/// <param name="Certificate">The certificate data.</param>
/// <param name="SignedAt">The signing time.</param>
/// <param name="FileName">The signature file name.</param>
/// <param name="Size">The signature file size.</param>
public sealed record DocumentSignatureItem(
    Guid Id,
    Guid UserId,
    string UserName,
    SignatureFormat Format,
    SignaturePackaging Packaging,
    string? Level,
    KeyMediaType KeyMedia,
    SignerCertificate Certificate,
    DateTimeOffset SignedAt,
    string FileName,
    long Size);
