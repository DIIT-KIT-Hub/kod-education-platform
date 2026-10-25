namespace KOD.Domain.Documents;

/// <summary>
/// Decision state of a single signer.
/// </summary>
public enum SignerStatus
{
    /// <summary>The signer has not acted yet.</summary>
    Pending = 0,

    /// <summary>The signer signed the document.</summary>
    Signed = 1,

    /// <summary>The signer declined to sign.</summary>
    Rejected = 2,
}
