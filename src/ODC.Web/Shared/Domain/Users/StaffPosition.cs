using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Shared.Domain.Users;

/// <summary>
/// A position held by a person in a unit of the organization (e.g. «начальник відділу продажу»): the unit, the role
/// (from the catalog of the master database), the official title and the primary flag. A person may hold the same
/// role in several units, so a position is identified by its own <see cref="Id"/>.
/// The position's role defines the permissions; the unit defines the scope of unit-level permissions.
/// </summary>
/// <remarks>Positions are changed only through <see cref="Member.SetPositions"/>.</remarks>
public sealed class StaffPosition
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaffPosition"/> class.
    /// </summary>
    /// <param name="userId">The person (member) identifier.</param>
    /// <param name="organizationUnitId">The unit identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="title">The official position title.</param>
    /// <param name="isPrimary">Whether this is the user's main position.</param>
    public StaffPosition(Guid userId, Guid organizationUnitId, Guid roleId, string? title, bool isPrimary)
    {
        UserId = userId;
        OrganizationUnitId = organizationUnitId;
        RoleId = roleId;
        Title = Guard.OptionalText(title, TitleMaxLength, "Посада");
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StaffPosition"/> class for EF Core materialization.
    /// </summary>
    private StaffPosition()
    {
    }

    /// <summary>
    /// Gets the identifier of the position (UUID v7, index friendly; also orders positions by creation).
    /// </summary>
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the identifier of the person holding the position (the account's identifier).
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the person holding the position.
    /// </summary>
    public Member? Member { get; private set; }

    /// <summary>
    /// Gets the unit identifier.
    /// </summary>
    public Guid OrganizationUnitId { get; private set; }

    /// <summary>
    /// Gets the unit.
    /// </summary>
    public OrganizationUnit? OrganizationUnit { get; private set; }

    /// <summary>
    /// Gets the role identifier (a role of the master database's catalog).
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the official position title (free text), falls back to the role name when empty.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is the user's main position.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Updates the position details.
    /// </summary>
    /// <param name="title">The new title.</param>
    /// <param name="isPrimary">The new primary flag.</param>
    public void Update(string? title, bool isPrimary)
    {
        Title = Guard.OptionalText(title, TitleMaxLength, "Посада");
        IsPrimary = isPrimary;
    }
}
