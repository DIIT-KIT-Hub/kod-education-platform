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
/// Resets a password: it is removed and the security stamp changes, so every sign-in ends at its next token refresh. The
/// owner chooses a new password through the activation (an enrolled authenticator is still asked for).
/// </summary>
public static class ResetUserPassword
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    [RequirePermission(Permissions.Users.Security)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Removes the password and tells the owner.
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

            var result = await identity.RemovePasswordAsync(user.Value);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            await audit.WriteAsync(AuditActions.PasswordReset, currentUser.Id, currentUser.DisplayName, $"Скинуто пароль {user.Value.Email}", cancellationToken);
            directory.NotifyPasswordReset(user.Value);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST /api/v1/users/{id}/password-resets</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => UserDirectory.MapUsers(api).MapPost("{id:guid}/password-resets", HandleAsync).RequirePermission(Permissions.Users.Security);

        /// <summary>
        /// Resets the password: it is removed, every sign-in ends and the owner chooses a new one through the activation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
