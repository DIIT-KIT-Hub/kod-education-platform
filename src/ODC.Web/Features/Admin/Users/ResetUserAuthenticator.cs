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
/// Removes the enrolled authenticator; the user enrolls a new one at the next sign-in.
/// </summary>
public static class ResetUserAuthenticator
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    [RequirePermission(Permissions.Users.Security)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Resets the authenticator.
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

            var result = await identity.ResetAuthenticatorAsync(user.Value);
            if (result.IsSuccess)
            {
                await audit.WriteAsync(AuditActions.AuthenticatorReset, currentUser.Id, currentUser.DisplayName, $"Скинуто автентифікатор {user.Value.Email}", cancellationToken);
            }

            return result;
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/users/{id}/authenticator</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => UserDirectory.MapUsers(api).MapDelete("{id:guid}/authenticator", HandleAsync).RequirePermission(Permissions.Users.Security);

        /// <summary>
        /// Removes the enrolled authenticator; the user enrolls a new one at the next sign-in.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
