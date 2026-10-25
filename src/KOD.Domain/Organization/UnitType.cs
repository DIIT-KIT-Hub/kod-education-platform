using System.Collections.Frozen;
using KOD.Domain.Common;

namespace KOD.Domain.Organization;

/// <summary>
/// A kind of structural unit defined by the administrator (organization, department, division, sector, branch…),
/// so the structure can follow the vocabulary of any organization regardless of its domain.
/// </summary>
public sealed class UnitType : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Code"/>.
    /// </summary>
    public const int CodeMaxLength = 32;

    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 128;

    /// <summary>
    /// Maximum length of <see cref="Description"/>.
    /// </summary>
    public const int DescriptionMaxLength = 512;

    /// <summary>
    /// Maximum length of <see cref="Icon"/>.
    /// </summary>
    public const int IconMaxLength = 32;

    /// <summary>
    /// The icon used when none is chosen.
    /// </summary>
    public const string DefaultIcon = "folder-tree";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitType"/> class.
    /// </summary>
    /// <param name="code">Unique short code (e.g. <c>DEPARTMENT</c>).</param>
    /// <param name="name">The user-facing name (e.g. «Департамент»).</param>
    /// <param name="isOrganization">Whether units of this type are organizations (may form the top level of the structure).</param>
    public UnitType(string code, string name, bool isOrganization)
    {
        Code = Guard.Code(code, CodeMaxLength);
        Name = Guard.Text(name, NameMaxLength, "Назва");
        IsOrganization = isOrganization;
        Icon = isOrganization ? "building-2" : DefaultIcon;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitType"/> class for EF Core materialization.
    /// </summary>
    private UnitType()
    {
        Code = string.Empty;
        Name = string.Empty;
        Icon = DefaultIcon;
    }

    /// <summary>
    /// Gets the icons of the design system sprite that can mark a unit type.
    /// </summary>
    public static FrozenSet<string> Icons { get; } = new[]
    {
        "building-2", "landmark", "network", "folder-tree", "layers", "briefcase", "users", "user-cog", "shield",
        "scroll-text", "stamp", "book-open", "graduation-cap", "flask-conical", "monitor", "settings", "newspaper",
        "map-pin", "sparkles", "file-text",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Gets the unique short code (upper-case latin letters, digits, dashes).
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the user-facing name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the optional explanation shown to administrators.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the icon name from <see cref="Icons"/>.
    /// </summary>
    public string Icon { get; private set; }

    /// <summary>
    /// Gets a value indicating whether units of this type are organizations: they may stand at the top of the structure
    /// and bound the «documents of the organization» scope.
    /// </summary>
    public bool IsOrganization { get; private set; }

    /// <summary>
    /// Gets the display order in lists.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Updates the type.
    /// </summary>
    /// <param name="code">The new unique code.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="icon">The icon name from <see cref="Icons"/>.</param>
    /// <param name="isOrganization">Whether units of this type are organizations.</param>
    /// <param name="sortOrder">The display order.</param>
    public void Update(string code, string name, string? description, string? icon, bool isOrganization, int sortOrder)
    {
        var iconName = string.IsNullOrWhiteSpace(icon) ? DefaultIcon : icon.Trim();
        if (!Icons.Contains(iconName))
        {
            throw new DomainException($"Невідома іконка «{iconName}».");
        }

        Code = Guard.Code(code, CodeMaxLength);
        Name = Guard.Text(name, NameMaxLength, "Назва");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        Icon = iconName;
        IsOrganization = isOrganization;
        SortOrder = sortOrder;
    }
}
