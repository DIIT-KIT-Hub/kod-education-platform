using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Auth;

/// <summary>
/// Signs out: revokes the refresh token family and removes the token cookies.
/// </summary>
public static class SignOut
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Command : ICommand<Result>;

    /// <summary>
    /// Records the sign-out and ends the sign-in.
    /// </summary>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(SignInManager<User> signInManager, ICurrentUser currentUser, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.Id is { } me)
            {
                await audit.WriteAsync(AuditActions.SignOut, me, currentUser.DisplayName, null, cancellationToken);
            }

            await signInManager.SignOutAsync();
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST /api/v1/auth/logout</c> (also with an expired sign-in).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapPost("logout", HandleAsync).AllowAnonymous();

        /// <summary>
        /// Signs out: revokes the refresh token family and removes the token cookies.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(), cancellationToken)).ToNoContent();
    }
}
