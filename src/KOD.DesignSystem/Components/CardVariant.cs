namespace KOD.DesignSystem.Components;

/// <summary>
/// Visual style of a <see cref="Card"/>.
/// </summary>
public enum CardVariant
{
    /// <summary>White surface with a hairline border.</summary>
    Default = 0,

    /// <summary>Gray surface without a border.</summary>
    Muted = 1,

    /// <summary>Black surface (white in dark theme).</summary>
    Inverse = 2,

    /// <summary>No inner padding (for grids and media).</summary>
    Flush = 3,
}
