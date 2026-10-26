using System.Globalization;

namespace KOD.Domain.Localization;

/// <summary>
/// The interface languages of the cabinet. Ukrainian is the default and the source language of every text.
/// </summary>
public static class Languages
{
    /// <summary>
    /// Ukrainian.
    /// </summary>
    public const string Ukrainian = "uk";

    /// <summary>
    /// English.
    /// </summary>
    public const string English = "en";

    /// <summary>
    /// Gets the supported language codes, the default first.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [Ukrainian, English];

    /// <summary>
    /// Gets the language of the current request (from <see cref="CultureInfo.CurrentUICulture"/>).
    /// </summary>
    public static string Current => Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ?? Ukrainian;

    /// <summary>
    /// Returns the supported language code that matches the value, or <see langword="null"/>.
    /// </summary>
    /// <param name="language">A language code or culture name (<c>en</c>, <c>en-GB</c>, <c>UK</c>).</param>
    /// <returns>The language code or <see langword="null"/>.</returns>
    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var code = language.Trim().Split('-', '_')[0].ToLowerInvariant();
        return All.Contains(code) ? code : null;
    }

    /// <summary>
    /// Returns the culture that formats dates and numbers for a language (Kyiv conventions: day first, 24-hour clock).
    /// </summary>
    /// <param name="language">The language code.</param>
    /// <returns>The culture name.</returns>
    public static string CultureOf(string language) => language == English ? "en-GB" : "uk-UA";

    /// <summary>
    /// Returns the name of a language in that language («Українська», «English»).
    /// </summary>
    /// <param name="language">The language code.</param>
    /// <returns>The native name.</returns>
    public static string NativeName(string language) => language == English ? "English" : "Українська";
}
