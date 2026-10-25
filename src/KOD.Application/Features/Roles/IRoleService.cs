using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Roles;

/// <summary>
/// Role and permission administration use cases.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Lists roles.
    /// </summary>
    /// <param name="request">The paging parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of roles.</returns>
    Task<Result<PagedResult<RoleListItem>>> ListAsync(PageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a role.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role.</returns>
    Task<Result<RoleDetails>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all roles for select inputs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The options.</returns>
    Task<IReadOnlyList<RoleOption>> GetOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a custom role.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new role identifier.</returns>
    Task<Result<Guid>> CreateAsync(SaveRoleCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a role and refreshes the sessions of its holders.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(Guid id, SaveRoleCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a custom role that nobody holds.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
