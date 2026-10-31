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
    /// The custom select drop-down and date picker script (loaded after <see cref="Script"/>).
    /// </summary>
    public const string PickersScript = "js/kod-pickers.js";

    /// <summary>
    /// The theme bootstrap script that must run in <c>&lt;head&gt;</c> before the first paint.
    /// </summary>
    public const string ThemeScript = "js/kod-theme.js";

    /// <summary>
    /// The icon sprite.
    /// </summary>
    public const string IconSprite = "icons/sprite.svg";

    /// <summary>
    /// Gets the variable font files to preload. They are preloaded by these plain paths (not fingerprinted), because
    /// the <c>@font-face</c> rules of <see cref="Stylesheet"/> reference them so.
    /// </summary>
    public static IReadOnlyList<string> Fonts { get; } =
    [
        "fonts/manrope-cyrillic.woff2",
        "fonts/manrope-latin.woff2",
    ];

    /// <summary>
    /// Returns the scripts with the client-side translations of a language (<c>window.kodI18n</c>, read by <c>kodDs.t</c>).
    /// They load before the behaviors scripts. Ukrainian is the source language and needs none.
    /// </summary>
    /// <param name="language">The language code.</param>
    /// <returns>The script paths.</returns>
    public static IReadOnlyList<string> TranslationScripts(string language) => language == KOD.Domain.Localization.Languages.English
        ? ["js/i18n/en.js", "js/i18n/en.help.js"]
        : [];
}
