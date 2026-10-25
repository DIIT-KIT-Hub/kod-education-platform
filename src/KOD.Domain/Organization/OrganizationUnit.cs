using KOD.Domain.Common;

namespace KOD.Domain.Organization;

/// <summary>
/// A node of the institution's organizational tree: university, institute, faculty, department or division.
/// </summary>
public sealed class OrganizationUnit : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 256;

    /// <summary>
    /// Maximum length of <see cref="ShortName"/>.
    /// </summary>
    public const int ShortNameMaxLength = 64;

    /// <summary>
    /// Maximum length of <see cref="Code"/>.
    /// </summary>
    public const int CodeMaxLength = 32;

    /// <summary>
    /// The child units.
    /// </summary>
    private readonly List<OrganizationUnit> _children = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationUnit"/> class.
    /// </summary>
    /// <param name="code">Unique short code used for imports (e.g. <c>FIT</c>).</param>
    /// <param name="name">Full official name.</param>
    /// <param name="type">The unit type.</param>
    /// <param name="parentId">The parent unit identifier, <see langword="null"/> for the root.</param>
    public OrganizationUnit(string code, string name, OrganizationUnitType type, Guid? parentId)
    {
        Code = NormalizeCode(code);
        Name = Guard.Text(name, NameMaxLength, "Назва");
        Type = type;
        ParentId = parentId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationUnit"/> class for EF Core materialization.
    /// </summary>
    private OrganizationUnit()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Gets the unique short code of the unit (upper-case latin letters, digits, dashes).
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the full official name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the abbreviation (e.g. «ФІТ»).
    /// </summary>
    public string? ShortName { get; private set; }

    /// <summary>
    /// Gets the unit type.
    /// </summary>
    public OrganizationUnitType Type { get; private set; }

    /// <summary>
    /// Gets the parent unit identifier.
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Gets the parent unit.
    /// </summary>
    public OrganizationUnit? Parent { get; private set; }

    /// <summary>
    /// Gets the direct child units.
    /// </summary>
    public IReadOnlyCollection<OrganizationUnit> Children => _children;

    /// <summary>
    /// Gets the display order among siblings.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the unit is active (inactive units are kept for history).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Updates the descriptive data of the unit.
    /// </summary>
    /// <param name="code">The new unique code.</param>
    /// <param name="name">The new full name.</param>
    /// <param name="shortName">The new abbreviation.</param>
    /// <param name="type">The new unit type.</param>
    /// <param name="sortOrder">The display order among siblings.</param>
    public void Update(string code, string name, string? shortName, OrganizationUnitType type, int sortOrder)
    {
        Code = NormalizeCode(code);
        Name = Guard.Text(name, NameMaxLength, "Назва");
        ShortName = Guard.OptionalText(shortName, ShortNameMaxLength, "Скорочена назва");
        Type = type;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Moves the unit under another parent.
    /// </summary>
    /// <param name="parentId">The new parent identifier.</param>
    /// <param name="descendantIds">Identifiers of all current descendants, used to prevent cycles.</param>
    public void MoveTo(Guid? parentId, IReadOnlySet<Guid> descendantIds)
    {
        ArgumentNullException.ThrowIfNull(descendantIds);

        if (parentId == Id || (parentId is { } id && descendantIds.Contains(id)))
        {
            throw new DomainException("Підрозділ не може бути вкладений сам у себе.");
        }

        ParentId = parentId;
    }

    /// <summary>
    /// Activates or deactivates the unit.
    /// </summary>
    /// <param name="isActive">The new state.</param>
    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>
    /// Validates and normalizes a unit code.
    /// </summary>
    /// <param name="code">The raw code.</param>
    /// <returns>The normalized upper-case code.</returns>
    private static string NormalizeCode(string code)
    {
        var value = Guard.Text(code, CodeMaxLength, "Код").ToUpperInvariant();
        return value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? value
            : throw new DomainException("Код може містити лише латинські літери, цифри, «-» та «_».");
    }
}
