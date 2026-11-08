using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Blocks or unblocks an account; a blocked account loses its sessions at once.
/// </summary>
public static class SetUserStatus
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    /// <param name="IsActive">Whether the account may sign in.</param>
    [RequirePermission(Permissions.Users.Manage)]
    public sealed record Command(Guid Id, bool IsActive) : ICommand<Result>;

    /// <summary>
    /// Changes the state of a whole account in the scope.
    /// </summary>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(
        ICurrentUser currentUser,
        IIdentityService identity,
        IAccessScope accessScope,
        UserDirectory directory,
        IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var (id, isActive) = (request.Id, request.IsActive);
            if (id == currentUser.Id && !isActive)
            {
                return Error.Validation(Texts.T("Ви не можете заблокувати власний обліковий запис."));
            }

            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
            var user = await directory.LoadForManagementAsync(id, scope, requireWholeAccount: true, cancellationToken);
            if (!user.IsSuccess)
            {
                return user.Error;
            }

            user.Value.SetActive(isActive);
            audit.Add(AuditActions.UserStatusChanged, nameof(User), id.ToString(), isActive ? $"Розблоковано {user.Value.Email}" : $"Заблоковано {user.Value.Email}");
            var result = await identity.UpdateAsync(user.Value);
            if (result.IsSuccess && !isActive)
            {
                await identity.InvalidateSessionsAsync(user.Value);
            }

            return result;
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/users/{id}/status</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => UserDirectory.MapUsers(api).MapPut("{id:guid}/status", HandleAsync).RequirePermission(Permissions.Users.Manage);

        /// <summary>
        /// Blocks or unblocks the account.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="status">The new state.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            UserStatusRequest status,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, status.IsActive), cancellationToken)).ToNoContent();
    }
}
