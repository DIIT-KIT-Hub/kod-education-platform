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
}
