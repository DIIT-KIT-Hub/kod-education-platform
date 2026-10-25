namespace KOD.DesignSystem;

/// <summary>
/// Paths of the design system static assets (served from <c>_content/KOD.DesignSystem</c>).
/// </summary>
public static class DesignTokens
{
    /// <summary>
    /// The base path of the design system static assets.
    /// </summary>
    public const string ContentPath = "_content/KOD.DesignSystem";

    /// <summary>
    /// The design system stylesheet.
    /// </summary>
    public const string Stylesheet = ContentPath + "/css/kod-ds.css";

    /// <summary>
    /// The design system behaviors script (dialogs, confirmations, toasts, drop zones, theme switch).
    /// </summary>
    public const string Script = ContentPath + "/js/kod-ds.js";

    /// <summary>
    /// The theme bootstrap script that must run in <c>&lt;head&gt;</c> before the first paint.
    /// </summary>
    public const string ThemeScript = ContentPath + "/js/kod-theme.js";

    /// <summary>
    /// The icon sprite.
    /// </summary>
    public const string IconSprite = ContentPath + "/icons/sprite.svg";

    /// <summary>
    /// Gets the variable font files to preload.
    /// </summary>
    public static IReadOnlyList<string> Fonts { get; } =
    [
        ContentPath + "/fonts/manrope-cyrillic.woff2",
        ContentPath + "/fonts/manrope-latin.woff2",
    ];
}
