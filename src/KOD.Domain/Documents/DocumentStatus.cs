namespace KOD.Domain.Documents;

/// <summary>
/// Life-cycle state of a document.
/// </summary>
public enum DocumentStatus
{
    /// <summary>The document is being prepared by its author.</summary>
    Draft = 0,

    /// <summary>The document is on its signing route.</summary>
    InSigning = 1,

    /// <summary>Every signer has signed the document.</summary>
    Signed = 2,

    /// <summary>A signer declined to sign the document.</summary>
    Rejected = 3,

    /// <summary>The document was moved to the archive.</summary>
    Archived = 4,
}
