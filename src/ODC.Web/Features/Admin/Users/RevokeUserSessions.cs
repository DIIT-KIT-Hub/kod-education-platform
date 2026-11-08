using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Ends all sessions of an account (and lifts a lockout).
/// </summary>
public static class RevokeUserSessions
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    [RequirePermission(Permissions.Users.Security)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Changes the security stamp and unlocks the account.
    /// </summary>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ICurrentUser currentUser, IIdentityService identity, UserDirectory directory, IAuditLogger audit)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await directory.LoadForSecurityAsync(request.Id, cancellationToken);
            if (!user.IsSuccess)
            {
                return user.Error;
            }

            await identity.InvalidateSessionsAsync(user.Value);
            await identity.UnlockAsync(user.Value);
            await audit.WriteAsync(AuditActions.SessionRevoked, currentUser.Id, currentUser.DisplayName, $"Завершено всі входи {user.Value.Email}", cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/users/{id}/sessions</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => UserDirectory.MapUsers(api).MapDelete("{id:guid}/sessions", HandleAsync).RequirePermission(Permissions.Users.Security);

        /// <summary>
        /// Ends all sessions of the user.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
