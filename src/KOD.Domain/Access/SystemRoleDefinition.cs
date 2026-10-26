using System.Text.Json.Serialization;
using KOD.Domain.Localization;

namespace KOD.Domain.Access;

/// <summary>
/// Describes a predefined role that is created on the first start of the system.
/// </summary>
/// <param name="Code">The stable role code.</param>
/// <param name="SourceName">The role name in Ukrainian: stored in the database when the role is seeded and used as the translation key.</param>
/// <param name="SourceDescription">The role description in Ukrainian: stored in the database when the role is seeded and used as the translation key.</param>
/// <param name="Permissions">The default permissions of the role.</param>
/// <param name="IsLeadership">Whether the role is a leadership position.</param>
public sealed record SystemRoleDefinition(
    string Code,
    [property: JsonIgnore] string SourceName,
    [property: JsonIgnore] string SourceDescription,
    IReadOnlyList<string> Permissions,
    bool IsLeadership)
{
    /// <summary>
    /// Gets the user-facing role name in the current interface language.
    /// </summary>
    public string Name => Texts.T(SourceName);

    /// <summary>
    /// Gets the user-facing role description in the current interface language.
    /// </summary>
    public string Description => Texts.T(SourceDescription);
}
