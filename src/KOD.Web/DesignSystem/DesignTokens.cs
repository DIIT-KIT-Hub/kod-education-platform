namespace KOD.Web.DesignSystem;

/// <summary>
/// Paths of the design system static assets (served from <c>wwwroot</c>).
/// </summary>
public static class DesignTokens
{
    /// <summary>
    /// The design system stylesheet.
    /// </summary>
    public const string Stylesheet = "css/kod-ds.css";

    /// <summary>
    /// The design system behaviors script (dialogs, confirmations, toasts, drop zones, theme switch).
    /// </summary>
    public const string Script = "js/kod-ds.js";

    /// <summary>
    /// The theme bootstrap script that must run in <c>&lt;head&gt;</c> before the first paint.
    /// </summary>
    public const string ThemeScript = "js/kod-theme.js";

    /// <summary>
    /// The icon sprite.
    /// </summary>
    public const string IconSprite = "icons/sprite.svg";

    /// <summary>
    /// Gets the variable font files to preload.
    /// </summary>
    public static IReadOnlyList<string> Fonts { get; } =
    [
        "fonts/manrope-cyrillic.woff2",
        "fonts/manrope-latin.woff2",
    ];
}
