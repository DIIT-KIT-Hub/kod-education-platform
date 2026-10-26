using KOD.Domain.Documents;

namespace KOD.Application.Abstractions;

/// <summary>
/// Structural (non-cryptographic) inspection of signature containers on the server.
/// The cryptographic verification is performed in the browser by the official IIT library.
/// </summary>
public interface ISignatureInspector
{
    /// <summary>
    /// Extracts the signer's certificate data from a signature container when the format allows it.
    /// </summary>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="format">The signature format.</param>
    /// <returns>The certificate data, or <see langword="null"/> when it cannot be extracted.</returns>
    SignerCertificate? TryReadSigner(ReadOnlyMemory<byte> signature, SignatureFormat format);

    /// <summary>
    /// Extracts the certificates of every signer of a CAdES container (one container may hold several signatures).
    /// </summary>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="format">The signature format.</param>
    /// <returns>The signers, or <see langword="null"/> when the format or the bytes cannot be parsed.</returns>
    IReadOnlyList<SignerCertificate>? TryReadSigners(ReadOnlyMemory<byte> signature, SignatureFormat format);

    /// <summary>
    /// Counts the signatures in a signed file: signers of a CAdES container, signature dictionaries of a PDF, signatures
    /// inside an ASiC container.
    /// </summary>
    /// <param name="signature">The signed file.</param>
    /// <param name="format">The signature format.</param>
    /// <returns>The number of signatures, or <see langword="null"/> when it cannot be determined.</returns>
    int? TryCountSignatures(ReadOnlyMemory<byte> signature, SignatureFormat format);
}
