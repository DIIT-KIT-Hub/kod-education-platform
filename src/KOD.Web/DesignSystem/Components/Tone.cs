namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Semantic tone of badges and alerts in the monochrome palette.
/// </summary>
public enum Tone
{
    /// <summary>Light gray surface.</summary>
    Neutral = 0,

    /// <summary>Black surface (white in dark theme) — completed, important.</summary>
    Inverse = 1,

    /// <summary>Solid outline — in progress, attention.</summary>
    Outline = 2,

    /// <summary>Dashed outline — draft, inactive.</summary>
    Dashed = 3,

    /// <summary>Red — errors and destructive states.</summary>
    Danger = 4,
}
