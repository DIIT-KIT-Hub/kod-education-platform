using KOD.Application.Common.Paging;

namespace KOD.Application.Features.Users;

/// <summary>
/// Paging and filter parameters of the user grid.
/// </summary>
public sealed record UserQuery : PageRequest
{
    /// <summary>
    /// Gets the account state filter.
    /// </summary>
    public UserStatusFilter Status { get; init; }

    /// <summary>
    /// Gets the unit filter; users with a position in the unit or any of its descendants match.
    /// </summary>
    public Guid? UnitId { get; init; }

    /// <summary>
    /// Gets the role filter.
    /// </summary>
    public Guid? RoleId { get; init; }
}
