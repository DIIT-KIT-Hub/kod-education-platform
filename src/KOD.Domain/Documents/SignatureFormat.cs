namespace KOD.Domain.Documents;

/// <summary>
/// Advanced electronic signature format (DSTU ETSI EN 319 122/132/142, ETSI EN 319 162).
/// </summary>
public enum SignatureFormat
{
    /// <summary>CMS Advanced Electronic Signature (*.p7s).</summary>
    CAdES = 0,

    /// <summary>XML Advanced Electronic Signature (*.xml).</summary>
    XAdES = 1,

    /// <summary>PDF Advanced Electronic Signature (signed *.pdf).</summary>
    PAdES = 2,

    /// <summary>Associated Signature Container, simple form — one file (*.asics).</summary>
    AsicS = 3,

    /// <summary>Associated Signature Container, extended form — one or more files (*.asice).</summary>
    AsicE = 4,
}
