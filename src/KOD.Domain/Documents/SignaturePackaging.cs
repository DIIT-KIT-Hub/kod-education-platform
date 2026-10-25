namespace KOD.Domain.Documents;

/// <summary>
/// Whether the signature contains the signed data.
/// </summary>
public enum SignaturePackaging
{
    /// <summary>The signature is stored separately from the data.</summary>
    Detached = 0,

    /// <summary>The signed data is embedded in the signature container.</summary>
    Enveloped = 1,
}
