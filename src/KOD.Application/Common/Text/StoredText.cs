using System.Diagnostics.CodeAnalysis;
using KOD.Domain.Localization;

namespace KOD.Application.Common.Text;

/// <summary>
/// Shows names stored in the database (role names, unit type names) in the interface language.
/// <para>
/// The names the system seeds in Ukrainian (system roles, the default unit types) have translations in the catalog;
/// names entered by administrators have none and pass through unchanged. Use only for display-only values — never for
/// values an editor form saves back. Inside EF Core queries call these methods only in the final projection.
/// </para>
/// </summary>
public static class StoredText
{
    /// <summary>
    /// Translates a stored name or description when the catalog knows it.
    /// </summary>
    /// <param name="text">The stored text.</param>
    /// <returns>The text in the interface language, or <see langword="null"/> when <paramref name="text"/> is <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? Display(string? text) => text is null ? null : Texts.T(text);

    /// <summary>
    /// Gets the shown title of a position: its official title, or the (translated) role name when it has none.
    /// </summary>
    /// <param name="title">The official position title.</param>
    /// <param name="roleName">The stored role name, or <see langword="null"/> when there is no position.</param>
    /// <returns>The title, or <see langword="null"/> when there is no position.</returns>
    public static string? PositionTitle(string? title, string? roleName) => roleName is null ? title : title ?? Display(roleName);

    /// <summary>
    /// Gets the shown description of a position: «title, unit».
    /// </summary>
    /// <param name="title">The official position title.</param>
    /// <param name="roleName">The stored role name, or <see langword="null"/> when there is no position.</param>
    /// <param name="unitName">The unit name, or <see langword="null"/> when there is no position.</param>
    /// <returns>The description, or <see langword="null"/> when there is no position.</returns>
    public static string? PositionInUnit(string? title, string? roleName, string? unitName)
        => roleName is null || unitName is null ? null : $"{title ?? Display(roleName)}, {unitName}";
}
