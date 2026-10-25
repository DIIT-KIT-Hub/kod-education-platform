using KOD.Application.Common.Paging;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Paging and filter parameters of the unit management grid.
/// </summary>
public sealed record UnitQuery : PageRequest
{
    /// <summary>
    /// Gets the unit type filter.
    /// </summary>
    public Guid? TypeId { get; init; }

    /// <summary>
    /// Gets the organization filter: the organization and all its units.
    /// </summary>
    public Guid? OrganizationId { get; init; }
}
