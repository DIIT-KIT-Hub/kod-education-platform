namespace KOD.Domain.Documents;

/// <summary>
/// Order in which signers are asked to sign.
/// </summary>
public enum SigningMode
{
    /// <summary>All signers may sign at the same time in any order.</summary>
    Parallel = 0,

    /// <summary>Signers sign one after another in the defined order.</summary>
    Sequential = 1,
}
