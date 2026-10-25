using KOD.Application.Common.Paging;
using KOD.Domain.Organization;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Paging and filter parameters of the unit management grid.
/// </summary>
public sealed record UnitQuery : PageRequest
{
    /// <summary>
    /// Gets the type filter.
    /// </summary>
    public OrganizationUnitType? Type { get; init; }
}
