using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Profile;

/// <summary>
/// Signs out on every other device: a new security stamp stops renewing the tokens issued before, and this browser gets
/// new ones.
/// </summary>
public static class SignOutEverywhere
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Command : ICommand<Result>;

    /// <summary>
    /// Changes the security stamp.
    /// </summary>
    /// <param name="account">The current user's account.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ProfileAccount account, IIdentityService identity, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            var user = await account.LoadAsync(cancellationToken);
            if (user is null)
            {
                return Error.Unauthorized();
            }

            await identity.InvalidateSessionsAsync(user);
            await audit.WriteAsync(AuditActions.SessionRevoked, user.Id, user.FullName, "Вихід на всіх інших пристроях", cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST /api/v1/profile/sign-out-everywhere</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ProfileAccount.MapProfile(api).MapPost("sign-out-everywhere", HandleAsync);

        /// <summary>
        /// Signs out on every other device: tokens issued before are no longer renewed, this browser gets new ones.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="principal">The signed-in user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            IRequestHandler<Command, Result> handler,
            ClaimsPrincipal principal,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(), cancellationToken);
            if (result.IsSuccess)
            {
                await ProfileAccount.RefreshSignInAsync(principal, userManager, signInManager);
            }

            return result.ToNoContent();
        }
    }
}
