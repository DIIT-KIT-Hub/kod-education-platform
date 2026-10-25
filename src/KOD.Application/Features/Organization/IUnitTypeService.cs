using KOD.Application.Common.Models;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Use cases of the unit type catalog (organization, department, division, sector…).
/// </summary>
public interface IUnitTypeService
{
    /// <summary>
    /// Lists every unit type with its usage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The types in display order.</returns>
    Task<Result<IReadOnlyList<UnitTypeItem>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the types for select inputs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The types in display order.</returns>
    Task<IReadOnlyList<UnitTypeRef>> GetOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a unit type.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The type.</returns>
    Task<Result<UnitTypeItem>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a unit type.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new type identifier.</returns>
    Task<Result<Guid>> CreateAsync(SaveUnitTypeCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a unit type.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(Guid id, SaveUnitTypeCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a unit type that no unit uses.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
