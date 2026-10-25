namespace KOD.Domain.Documents;

/// <summary>
/// Storage type of the private key used for signing.
/// </summary>
public enum KeyMediaType
{
    /// <summary>File key container (Key-6.dat, *.pfx, *.jks, *.zs2).</summary>
    File = 0,

    /// <summary>Protected hardware media (token, smart card).</summary>
    Hardware = 1,

    /// <summary>Remote (cloud) key held by a qualified trust service provider.</summary>
    Cloud = 2,
}
