using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// User accounts. Accounts are created by administrators only (no self-registration).
/// </summary>
/// <param name="users">The user service.</param>
/// <param name="organization">The organization service.</param>
/// <param name="roles">The role service.</param>
[Route("api/v1/users")]
[RequirePermission(Permissions.Users.View)]
public sealed class UsersController(IUserService users, IOrganizationService organization, IRoleService roles) : ApiControllerBase
{
    /// <summary>
    /// Lists the units of the user filters (organizations the user sees) or of the position editor (organizations the
    /// user manages), in tree order.
    /// </summary>
    /// <param name="manage">Whether the units are for assigning positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units.</returns>
    [HttpGet("units")]
    public async Task<ActionResult<IReadOnlyList<UnitOption>>> Units([FromQuery] bool manage, CancellationToken cancellationToken)
        => Ok(await organization.GetOptionsAsync(manage ? Permissions.Users.Manage : Permissions.Users.View, cancellationToken));

    /// <summary>
    /// Lists the roles for the filters and the position editor.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles.</returns>
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleOption>>> Roles(CancellationToken cancellationToken)
        => Ok(await roles.GetOptionsAsync(cancellationToken));

    /// <summary>
    /// Lists user accounts.
    /// </summary>
    /// <param name="query">Paging and filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of users.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItem>>> List([FromQuery] UserQuery query, CancellationToken cancellationToken)
        => FromResult(await users.ListAsync(query, cancellationToken));

    /// <summary>
    /// Gets a user account.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await users.GetAsync(id, cancellationToken));

    /// <summary>
    /// Creates an account; the response contains a one-time temporary password.
    /// </summary>
    /// <param name="command">The account data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the one-time credentials.</returns>
    [HttpPost]
    [RequirePermission(Permissions.Users.Manage)]
    [ProducesResponseType<UserCredentials>(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create(SaveUserCommand command, CancellationToken cancellationToken)
    {
        var result = await users.CreateAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.UserId }, result.Value) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces the account data and positions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The account data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.Users.Manage)]
    public async Task<ActionResult> Update(Guid id, SaveUserCommand command, CancellationToken cancellationToken)
        => FromResult(await users.UpdateAsync(id, command, cancellationToken));

    /// <summary>
    /// Blocks or unblocks the account.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="request">The new state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/status")]
    [RequirePermission(Permissions.Users.Manage)]
    public async Task<ActionResult> SetStatus(Guid id, UserStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await users.SetActiveAsync(id, request.IsActive, cancellationToken));
    }

    /// <summary>
    /// Issues a new temporary password (a password reset is a new resource).
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the one-time credentials.</returns>
    [HttpPost("{id:guid}/password-resets")]
    [RequirePermission(Permissions.Users.Security)]
    [ProducesResponseType<UserCredentials>(StatusCodes.Status201Created)]
    public async Task<ActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var result = await users.ResetPasswordAsync(id, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id }, result.Value) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Removes the enrolled authenticator; the user enrolls a new one at the next sign-in.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}/authenticator")]
    [RequirePermission(Permissions.Users.Security)]
    public async Task<ActionResult> ResetAuthenticator(Guid id, CancellationToken cancellationToken)
        => FromResult(await users.ResetAuthenticatorAsync(id, cancellationToken));

    /// <summary>
    /// Ends all sessions of the user.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}/sessions")]
    [RequirePermission(Permissions.Users.Security)]
    public async Task<ActionResult> RevokeSessions(Guid id, CancellationToken cancellationToken)
        => FromResult(await users.RevokeSessionsAsync(id, cancellationToken));
}
