using ODC.Web.Shared.Text;

namespace ODC.Web.Shared.Access;

/// <summary>
/// A position a person holds, with the role's stored name.
/// </summary>
/// <param name="UserId">The person.</param>
/// <param name="UnitId">The unit.</param>
/// <param name="UnitName">The unit's name.</param>
/// <param name="RoleId">The role.</param>
/// <param name="RoleName">The role's stored (untranslated) name.</param>
/// <param name="IsLeadership">Whether the role is a leadership role.</param>
/// <param name="Title">The official title, if any.</param>
/// <param name="IsPrimary">Whether it is the person's main position.</param>
internal sealed record HeldPosition(Guid UserId, Guid UnitId, string UnitName, Guid RoleId, string RoleName, bool IsLeadership, string? Title, bool IsPrimary)
{
    /// <summary>
    /// Gets the shown title: the official one, or the translated role name.
    /// </summary>
    public string ShownTitle => StoredText.PositionTitle(Title, RoleName) ?? string.Empty;

    /// <summary>
    /// Gets the shown description: «title, unit».
    /// </summary>
    public string InUnit => StoredText.PositionInUnit(Title, RoleName, UnitName) ?? string.Empty;
}
