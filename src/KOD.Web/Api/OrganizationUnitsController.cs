using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The organizational structure: university, institutes, faculties, departments, divisions.
/// </summary>
/// <param name="organization">The organization service.</param>
[Route("api/v1/organization-units")]
[RequirePermission(Permissions.Structure.View)]
public sealed class OrganizationUnitsController(IOrganizationService organization) : ApiControllerBase
{
    /// <summary>
    /// Gets the organization tree.
    /// </summary>
    /// <param name="includeInactive">Whether inactive units are included.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The root nodes.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitTreeNode>>> Tree([FromQuery] bool includeInactive, CancellationToken cancellationToken)
        => FromResult(await organization.GetTreeAsync(includeInactive, cancellationToken));

    /// <summary>
    /// Gets a unit.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unit.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await organization.GetAsync(id, cancellationToken));

    /// <summary>
    /// Lists the staff of a unit.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="request">Paging and search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of staff members.</returns>
    [HttpGet("{id:guid}/staff")]
    public async Task<ActionResult<PagedResult<UnitStaffItem>>> Staff(Guid id, [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => FromResult(await organization.GetStaffAsync(id, request, cancellationToken));

    /// <summary>
    /// Creates a unit.
    /// </summary>
    /// <param name="command">The unit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the new identifier.</returns>
    [HttpPost]
    [RequirePermission(Permissions.Structure.Manage)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create(SaveUnitCommand command, CancellationToken cancellationToken)
    {
        var result = await organization.CreateAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value }) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces a unit.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The unit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.Structure.Manage)]
    public async Task<ActionResult> Update(Guid id, SaveUnitCommand command, CancellationToken cancellationToken)
        => FromResult(await organization.UpdateAsync(id, command, cancellationToken));
}
