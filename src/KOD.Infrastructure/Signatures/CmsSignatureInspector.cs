using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using KOD.Application.Abstractions;
using KOD.Domain.Documents;

namespace KOD.Infrastructure.Signatures;

/// <summary>
/// Extracts the signer certificate from CAdES (CMS) containers. Only ASN.1 decoding is performed:
/// DSTU 4145 / GOST 34.311 cryptography is verified in the browser by the official IIT library.
/// </summary>
internal sealed class CmsSignatureInspector : ISignatureInspector
{
    /// <summary>
    /// The X.520 serialNumber attribute that carries «TINUA-…» / «EDRPOU…» identifiers in Ukrainian certificates.
    /// </summary>
    private const string SerialNumberOid = "2.5.4.5";

    /// <summary>
    /// The X.520 organizationName attribute.
    /// </summary>
    private const string OrganizationOid = "2.5.4.10";

    /// <inheritdoc />
    public SignerCertificate? TryReadSigner(ReadOnlyMemory<byte> signature, SignatureFormat format)
    {
        if (format != SignatureFormat.CAdES)
        {
            return null;
        }

        try
        {
            var cms = new SignedCms();
            cms.Decode(signature.Span);
            var certificate = cms.SignerInfos.Count > 0 ? cms.SignerInfos[0].Certificate : null;
            certificate ??= cms.Certificates.Count > 0 ? cms.Certificates[0] : null;
            if (certificate is null)
            {
                return null;
            }

            return new SignerCertificate(
                certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
                FindAttribute(certificate.SubjectName, OrganizationOid),
                FindAttribute(certificate.SubjectName, SerialNumberOid),
                certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true),
                certificate.SerialNumber,
                new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
                new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the first value of an attribute in a distinguished name.
    /// </summary>
    /// <param name="name">The distinguished name.</param>
    /// <param name="oid">The attribute OID.</param>
    /// <returns>The value or <see langword="null"/>.</returns>
    private static string? FindAttribute(X500DistinguishedName name, string oid)
        => name.EnumerateRelativeDistinguishedNames()
            .Where(static rdn => !rdn.HasMultipleElements)
            .FirstOrDefault(rdn => rdn.GetSingleElementType().Value == oid)
            ?.GetSingleElementValue();
}
