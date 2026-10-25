using KOD.Domain.Access;
using KOD.Domain.Common;
using KOD.Domain.Organization;

namespace KOD.Domain.Users;

/// <summary>
/// A position held by a user in an organization unit (e.g. «доцент кафедри програмної інженерії»).
/// The position's role defines the permissions; the unit defines the scope of unit-level permissions.
/// </summary>
public sealed class StaffPosition : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaffPosition"/> class.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
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
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the user.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the unit identifier.
    /// </summary>
    public Guid OrganizationUnitId { get; private set; }

    /// <summary>
    /// Gets the unit.
    /// </summary>
    public OrganizationUnit? OrganizationUnit { get; private set; }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the role.
    /// </summary>
    public Role? Role { get; private set; }

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
