namespace KOD.Domain.Documents;

/// <summary>
/// How the signatures of one document come together. In the cumulative formats every signer adds a signature to the file
/// signed before them, so the newest signature file holds all signatures of the document: one CAdES container (.p7s)
/// with several signers, one PDF with incremental signatures (PAdES), one ASiC container. XAdES has no append operation
/// in the signing library: its signatures are independent, each over the same document file.
/// </summary>
public static class SignatureChain
{
    /// <summary>
    /// How long a signer holds the turn to add the next signature; the others wait (see <see cref="Document.BeginSigning"/>).
    /// </summary>
    public static readonly TimeSpan TurnDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Determines whether signatures of the format are added to one container.
    /// </summary>
    /// <param name="format">The signature format.</param>
    /// <returns><see langword="true"/> for CAdES, PAdES, ASiC-S and ASiC-E.</returns>
    public static bool IsCumulative(SignatureFormat format) => format != SignatureFormat.XAdES;
}
