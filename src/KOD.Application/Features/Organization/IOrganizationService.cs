using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Organizational structure use cases.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Gets the whole organization tree.
    /// </summary>
    /// <param name="includeInactive">Whether inactive units are included.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The root nodes.</returns>
    Task<Result<IReadOnlyList<UnitTreeNode>>> GetTreeAsync(bool includeInactive, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a unit.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unit.</returns>
    Task<Result<UnitDetails>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the staff of a unit.
    /// </summary>
    /// <param name="id">The unit identifier.</param>
    /// <param name="request">The paging parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of staff members.</returns>
    Task<Result<PagedResult<UnitStaffItem>>> GetStaffAsync(Guid id, PageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists units for the management grid.
    /// </summary>
    /// <param name="query">The paging and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of units.</returns>
    Task<Result<PagedResult<UnitListItem>>> ListAsync(UnitQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all units as a flat hierarchical list for select inputs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The options.</returns>
    Task<IReadOnlyList<UnitOption>> GetOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a unit.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new unit identifier.</returns>
    Task<Result<Guid>> CreateAsync(SaveUnitCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a unit.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(Guid id, SaveUnitCommand command, CancellationToken cancellationToken);
}
