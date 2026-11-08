using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Paging and filter parameters of the user grid (also the filters of the export).
/// </summary>
public record UserQuery : PageRequest
{
    /// <summary>
    /// Gets the account state filter (all accounts when not given).
    /// </summary>
    public UserStatusFilter? Status { get; init; }

    /// <summary>
    /// Gets the unit filter; users with a position in the unit or any of its descendants match.
    /// </summary>
    public Guid? UnitId { get; init; }

    /// <summary>
    /// Gets the role filter.
    /// </summary>
    public Guid? RoleId { get; init; }
}
