namespace KOD.Domain.Documents;

/// <summary>
/// Kind of an organizational and administrative document (the common set of Ukrainian office work, DSTU 4163).
/// </summary>
public enum DocumentKind
{
    /// <summary>Order (наказ).</summary>
    Order = 0,

    /// <summary>Directive (розпорядження).</summary>
    Directive = 1,

    /// <summary>Internal memo (службова записка).</summary>
    Memo = 2,

    /// <summary>Application / statement (заява).</summary>
    Application = 3,

    /// <summary>Report (звіт).</summary>
    Report = 4,

    /// <summary>Meeting minutes (протокол).</summary>
    Minutes = 5,

    /// <summary>Contract or agreement (договір).</summary>
    Contract = 6,

    /// <summary>Certificate / reference (довідка).</summary>
    Certificate = 7,

    /// <summary>Regulation, statute or instruction (положення, інструкція).</summary>
    Regulation = 8,

    /// <summary>Incoming or outgoing letter (лист).</summary>
    Letter = 9,

    /// <summary>Act (акт): acceptance, inspection, write-off.</summary>
    Act = 10,

    /// <summary>Any other document.</summary>
    Other = 99,
}
