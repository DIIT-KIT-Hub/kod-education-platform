using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using KOD.Application.Abstractions;
using KOD.Domain.Documents;

namespace KOD.Infrastructure.Signatures;

/// <summary>
/// Structural inspection of signed files: signer certificates of CAdES (CMS) containers and the number of signatures in
/// CAdES, PDF and ASiC files. Only parsing is performed: DSTU 4145 / GOST 34.311 cryptography is verified in the browser
/// by the official IIT library.
/// </summary>
internal sealed partial class CmsSignatureInspector : ISignatureInspector
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
        => TryReadSigners(signature, format) is [var first, ..] ? first : null;

    /// <inheritdoc />
    public IReadOnlyList<SignerCertificate>? TryReadSigners(ReadOnlyMemory<byte> signature, SignatureFormat format)
    {
        if (format != SignatureFormat.CAdES || TryDecode(signature.Span) is not { } cms)
        {
            return null;
        }

        var signers = new List<SignerCertificate>();
        foreach (var signer in cms.SignerInfos)
        {
            if (signer.Certificate is { } certificate)
            {
                signers.Add(Describe(certificate));
            }
        }

        if (signers.Count == 0 && cms.Certificates.Count > 0)
        {
            signers.Add(Describe(cms.Certificates[0]));
        }

        return signers;
    }

    /// <inheritdoc />
    public int? TryCountSignatures(ReadOnlyMemory<byte> signature, SignatureFormat format) => format switch
    {
        SignatureFormat.CAdES => TryDecode(signature.Span)?.SignerInfos.Count,
        SignatureFormat.PAdES => CountPdfSignatures(signature.Span),
        SignatureFormat.AsicS or SignatureFormat.AsicE => TryCountAsicSignatures(signature),
        _ => null,
    };

    /// <summary>
    /// Decodes a CMS container.
    /// </summary>
    /// <param name="signature">The bytes.</param>
    /// <returns>The container, or <see langword="null"/> when the bytes are not CMS.</returns>
    private static SignedCms? TryDecode(ReadOnlySpan<byte> signature)
    {
        try
        {
            var cms = new SignedCms();
            cms.Decode(signature);
            return cms;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    /// <summary>
    /// Counts the signature dictionaries of a PDF (each one has a /ByteRange).
    /// </summary>
    /// <param name="pdf">The PDF bytes.</param>
    /// <returns>The number of signatures.</returns>
    private static int CountPdfSignatures(ReadOnlySpan<byte> pdf)
    {
        ReadOnlySpan<byte> marker = "/ByteRange"u8;
        var count = 0;
        for (var index = pdf.IndexOf(marker); index >= 0;)
        {
            count++;
            var next = pdf[(index + marker.Length)..].IndexOf(marker);
            index = next < 0 ? -1 : index + marker.Length + next;
        }

        return count;
    }

    /// <summary>
    /// Counts the signatures inside an ASiC container: signers of the CAdES files and signature elements of the XAdES
    /// files in META-INF.
    /// </summary>
    /// <param name="container">The ZIP bytes.</param>
    /// <returns>The number of signatures, or <see langword="null"/> when the container cannot be read.</returns>
    private static int? TryCountAsicSignatures(ReadOnlyMemory<byte> container)
    {
        try
        {
            using var zip = new ZipArchive(new MemoryStream(container.ToArray(), writable: false), ZipArchiveMode.Read);
            var count = 0;
            foreach (var entry in zip.Entries.Where(static e => e.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = entry.Open();
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                if (entry.FullName.EndsWith(".p7s", StringComparison.OrdinalIgnoreCase))
                {
                    count += TryDecode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))?.SignerInfos.Count ?? 0;
                }
                else if (entry.FullName.Contains("signature", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    count += XmlSignatureElement().Count(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
                }
            }

            return count;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// Describes a signer certificate.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The certificate data.</returns>
    private static SignerCertificate Describe(X509Certificate2 certificate) => new(
        certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
        FindAttribute(certificate.SubjectName, OrganizationOid),
        FindAttribute(certificate.SubjectName, SerialNumberOid),
        certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true),
        certificate.SerialNumber,
        new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
        new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero));

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

    /// <summary>
    /// An XML-DSig <c>Signature</c> start tag (with or without a namespace prefix).
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex(@"<(?:\w+:)?Signature[\s>]", RegexOptions.CultureInvariant)]
    private static partial Regex XmlSignatureElement();
}
