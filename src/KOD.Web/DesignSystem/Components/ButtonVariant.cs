namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Visual style of a <see cref="Button"/>.
/// </summary>
public enum ButtonVariant
{
    /// <summary>Filled black (white in dark theme) — the main action.</summary>
    Primary = 0,

    /// <summary>Outlined — a secondary action.</summary>
    Secondary = 1,

    /// <summary>Text-only — a tertiary action.</summary>
    Ghost = 2,

    /// <summary>Filled red — a destructive action.</summary>
    Danger = 3,
}
