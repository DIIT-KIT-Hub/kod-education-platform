using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// The second step of a sign-in for a user with an authenticator: the state of the pending sign-in, then a TOTP code
/// (single use) or a one-time recovery code.
/// </summary>
public static class CompleteSignIn
{
    /// <summary>
    /// The pending sign-in.
    /// </summary>
    public sealed record PendingQuery : IQuery<Result<PendingSignInResponse>>;

    /// <summary>
    /// A code of the authenticator app.
    /// </summary>
    /// <param name="Code">The code.</param>
    /// <param name="RememberMe">Whether the sign-in survives the browser session.</param>
    public sealed record AuthenticatorCommand(string? Code, bool RememberMe) : ICommand<Result>;

    /// <summary>
    /// A recovery code instead of the authenticator.
    /// </summary>
    /// <param name="Code">The recovery code.</param>
    public sealed record RecoveryCommand(string? Code) : ICommand<Result>;

    /// <summary>
    /// Describes the pending sign-in.
    /// </summary>
    /// <param name="flow">The shared sign-in steps.</param>
    internal sealed class PendingHandler(SignInFlow flow) : IRequestHandler<PendingQuery, Result<PendingSignInResponse>>
    {
        /// <inheritdoc />
        public async Task<Result<PendingSignInResponse>> HandleAsync(PendingQuery request, CancellationToken cancellationToken)
        {
            var user = await flow.PendingUserAsync();
            return user is null ? SignInFlow.NoPendingSignIn() : new PendingSignInResponse(user.TwoFactorEnabled);
        }
    }

    /// <summary>
    /// Signs in with the authenticator code.
    /// </summary>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="flow">The shared sign-in steps.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class AuthenticatorHandler(SignInManager<User> signInManager, SignInFlow flow, IAuditLogger audit) : IRequestHandler<AuthenticatorCommand, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(AuthenticatorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (await flow.PendingUserAsync() is not { TwoFactorEnabled: true } user)
            {
                return SignInFlow.NoPendingSignIn();
            }

            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(SignInFlow.NormalizeCode(request.Code), request.RememberMe, rememberClient: false);
            if (result.Succeeded)
            {
                await flow.RecordSignInAsync(user, "Пароль + автентифікатор", cancellationToken);
                return Result.Success();
            }

            if (result.IsLockedOut)
            {
                await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, null, cancellationToken);
                return SignInFlow.LockedOut();
            }

            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний код автентифікатора", cancellationToken);
            return Error.Validation(Texts.T("Код неправильний або вже недійсний. Спробуйте наступний код."));
        }
    }

    /// <summary>
    /// Signs in with a recovery code (each works once).
    /// </summary>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="flow">The shared sign-in steps.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class RecoveryHandler(SignInManager<User> signInManager, SignInFlow flow, IAuditLogger audit) : IRequestHandler<RecoveryCommand, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(RecoveryCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (await flow.PendingUserAsync() is not { TwoFactorEnabled: true } user)
            {
                return SignInFlow.NoPendingSignIn();
            }

            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync((request.Code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal));
            if (result.Succeeded)
            {
                await flow.RecordSignInAsync(user, "Пароль + резервний код", cancellationToken);
                return Result.Success();
            }

            if (result.IsLockedOut)
            {
                return SignInFlow.LockedOut();
            }

            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний резервний код", cancellationToken);
            return Error.Validation(Texts.T("Резервний код недійсний."));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/auth/login/pending</c>, <c>POST …/login/two-factor</c> and <c>POST …/login/recovery</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var auth = AuthApi.MapAuth(api);
            auth.MapGet("login/pending", PendingAsync).AllowAnonymous();
            auth.MapPost("login/two-factor", TwoFactorAsync).AllowAnonymous();
            auth.MapPost("login/recovery", RecoveryAsync).AllowAnonymous();
        }

        /// <summary>
        /// Describes the sign-in waiting for the second factor.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The pending sign-in, or 401 when there is none.</returns>
        private static async Task<Results<Ok<PendingSignInResponse>, ProblemHttpResult>> PendingAsync(IRequestHandler<PendingQuery, Result<PendingSignInResponse>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new PendingQuery(), cancellationToken)).ToOk();

        /// <summary>
        /// Completes the sign-in with an authenticator code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 when signed in.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> TwoFactorAsync(CodeRequest code, IRequestHandler<AuthenticatorCommand, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new AuthenticatorCommand(code.Code, code.RememberMe), cancellationToken)).ToNoContent();

        /// <summary>
        /// Completes the sign-in with a one-time recovery code instead of the authenticator.
        /// </summary>
        /// <param name="code">The recovery code.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 when signed in.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> RecoveryAsync(CodeRequest code, IRequestHandler<RecoveryCommand, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new RecoveryCommand(code.Code), cancellationToken)).ToNoContent();
    }
}
