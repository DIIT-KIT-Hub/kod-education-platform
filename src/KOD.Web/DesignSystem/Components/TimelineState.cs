namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// State of a <see cref="TimelineItem"/>.
/// </summary>
public enum TimelineState
{
    /// <summary>Not reached yet.</summary>
    Pending = 0,

    /// <summary>Active right now.</summary>
    Current = 1,

    /// <summary>Completed.</summary>
    Done = 2,

    /// <summary>Failed or declined.</summary>
    Failed = 3,
}
