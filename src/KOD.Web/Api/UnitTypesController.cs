using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The unit type catalog: organization, department, division, sector, branch and any custom kinds.
/// </summary>
/// <param name="unitTypes">The unit type service.</param>
[Route("api/v1/unit-types")]
[RequirePermission(Permissions.Structure.Manage)]
public sealed class UnitTypesController(IUnitTypeService unitTypes) : ApiControllerBase
{
    /// <summary>
    /// Lists the unit types with their usage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The types in display order.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitTypeItem>>> List(CancellationToken cancellationToken)
        => FromResult(await unitTypes.ListAsync(cancellationToken));

    /// <summary>
    /// Gets a unit type.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The type.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitTypeItem>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await unitTypes.GetAsync(id, cancellationToken));

    /// <summary>
    /// Creates a unit type.
    /// </summary>
    /// <param name="command">The type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the new identifier.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create(SaveUnitTypeCommand command, CancellationToken cancellationToken)
    {
        var result = await unitTypes.CreateAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value }) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces a unit type.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, SaveUnitTypeCommand command, CancellationToken cancellationToken)
        => FromResult(await unitTypes.UpdateAsync(id, command, cancellationToken));

    /// <summary>
    /// Deletes a unit type that no unit uses.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await unitTypes.DeleteAsync(id, cancellationToken));
}
