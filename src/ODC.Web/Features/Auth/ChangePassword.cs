using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// Changes the password of the signed-in user. The new security stamp ends the other sign-ins; this browser gets new
/// tokens.
/// </summary>
public static class ChangePassword
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="CurrentPassword">The current password.</param>
    /// <param name="NewPassword">The new password (Identity validators, breached-password check).</param>
    public sealed record Command(string CurrentPassword, string NewPassword) : ICommand<Result>;

    /// <summary>
    /// Changes the password.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(MasterDbContext db, ICurrentUser currentUser, IIdentityService identity, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;
            if (user is null)
            {
                return Error.Unauthorized();
            }

            if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
            {
                return Error.Validation(Texts.T("Новий пароль має відрізнятися від поточного."));
            }

            var result = await identity.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.IsSuccess)
            {
                await audit.WriteAsync(AuditActions.PasswordChanged, user.Id, user.FullName, null, cancellationToken);
            }

            return result;
        }
    }

    /// <summary>
    /// <c>POST /api/v1/auth/password</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapPost("password", HandleAsync);

        /// <summary>
        /// Changes the password of the signed-in user and re-issues the tokens of this browser; the other sign-ins end.
        /// </summary>
        /// <param name="passwords">The passwords.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="principal">The signed-in user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 when changed.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            ChangePasswordRequest passwords,
            IRequestHandler<Command, Result> handler,
            ClaimsPrincipal principal,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(passwords.CurrentPassword ?? string.Empty, passwords.NewPassword ?? string.Empty), cancellationToken);
            if (result.IsSuccess && await userManager.GetUserAsync(principal) is { } user)
            {
                await signInManager.RefreshSignInAsync(user);
            }

            return result.ToNoContent();
        }
    }
}
