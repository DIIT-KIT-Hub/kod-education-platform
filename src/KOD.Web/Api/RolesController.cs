using KOD.Application.Common.Paging;
using KOD.Application.Features.Roles;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Roles and their permissions.
/// </summary>
/// <param name="roles">The role service.</param>
[Route("api/v1/roles")]
[RequirePermission(Permissions.Roles.Manage)]
public sealed class RolesController(IRoleService roles) : ApiControllerBase
{
    /// <summary>
    /// Lists roles.
    /// </summary>
    /// <param name="request">Paging and search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of roles.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RoleListItem>>> List([FromQuery] PageRequest request, CancellationToken cancellationToken)
        => FromResult(await roles.ListAsync(request, cancellationToken));

    /// <summary>
    /// Gets a role.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await roles.GetAsync(id, cancellationToken));

    /// <summary>
    /// Creates a custom role.
    /// </summary>
    /// <param name="command">The role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the new identifier.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create(SaveRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await roles.CreateAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value }) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces a role's name, description and permissions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, SaveRoleCommand command, CancellationToken cancellationToken)
        => FromResult(await roles.UpdateAsync(id, command, cancellationToken));

    /// <summary>
    /// Deletes a custom role that nobody holds.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await roles.DeleteAsync(id, cancellationToken));
}
