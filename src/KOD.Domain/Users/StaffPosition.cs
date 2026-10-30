using KOD.Domain.Access;
using KOD.Domain.Common;
using KOD.Domain.Organization;
using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Users;

/// <summary>
/// A position held by a user in an organization unit (e.g. «начальник відділу продажу»): the Identity user role
/// (table <c>user_roles</c>) extended with the unit, the official title and the primary flag. A user may hold the same
/// role in several units, so a position is identified by its own <see cref="Id"/>.
/// The position's role defines the permissions; the unit defines the scope of unit-level permissions.
/// </summary>
/// <remarks>
/// Positions are changed only through <see cref="User.SetPositions"/>: <see cref="UserManager{TUser}.AddToRoleAsync"/>
/// cannot choose a unit. The read operations of the user manager (<c>GetRolesAsync</c>, <c>IsInRoleAsync</c>,
/// <c>GetUsersInRoleAsync</c>) work as usual.
/// </remarks>
public sealed class StaffPosition : IdentityUserRole<Guid>
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaffPosition"/> class for EF Core and the generic constraint of the
    /// Identity user store.
    /// </summary>
    public StaffPosition()
    {
    }

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
    /// Gets the identifier of the position (UUID v7, index friendly; also orders positions by creation).
    /// </summary>
    public Guid Id { get; private init; } = Guid.CreateVersion7();

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
