using System.Text.Json.Serialization;
using KOD.Domain.Localization;

namespace KOD.Domain.Access;

/// <summary>
/// Describes a single permission of the system.
/// </summary>
/// <param name="Code">The stable machine-readable code (e.g. <c>documents.sign</c>).</param>
/// <param name="Module">The module the permission belongs to.</param>
/// <param name="SourceTitle">The short user-facing title in Ukrainian (the translation key).</param>
/// <param name="SourceDescription">The user-facing explanation of what the permission allows, in Ukrainian (the translation key).</param>
public sealed record PermissionDefinition(
    string Code,
    AppModule Module,
    [property: JsonIgnore] string SourceTitle,
    [property: JsonIgnore] string SourceDescription)
{
    /// <summary>
    /// Gets the short user-facing title in the current interface language.
    /// </summary>
    public string Title => Texts.T(SourceTitle);

    /// <summary>
    /// Gets the user-facing explanation of what the permission allows, in the current interface language.
    /// </summary>
    public string Description => Texts.T(SourceDescription);
}
