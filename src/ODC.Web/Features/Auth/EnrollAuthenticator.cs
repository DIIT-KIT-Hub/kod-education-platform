using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// The second step of a sign-in for a user without an authenticator: the key with its QR code (created once), then the
/// first code enables two-factor authentication, issues the recovery codes and signs in.
/// </summary>
public static class EnrollAuthenticator
{
    /// <summary>
    /// The authenticator key of the pending user.
    /// </summary>
    public sealed record KeyQuery : IQuery<Result<AuthenticatorSetupResponse>>;

    /// <summary>
    /// The first code of the new authenticator.
    /// </summary>
    /// <param name="Code">The code.</param>
    /// <param name="RememberMe">Whether the sign-in survives the browser session.</param>
    public sealed record Command(string? Code, bool RememberMe) : ICommand<Result<RecoveryCodesResponse>>;

    /// <summary>
    /// Creates the key once (later requests show the same one).
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="flow">The shared sign-in steps.</param>
    internal sealed class KeyHandler(UserManager<User> userManager, SignInFlow flow) : IRequestHandler<KeyQuery, Result<AuthenticatorSetupResponse>>
    {
        /// <inheritdoc />
        public async Task<Result<AuthenticatorSetupResponse>> HandleAsync(KeyQuery request, CancellationToken cancellationToken)
        {
            if (await flow.PendingUserAsync() is not { TwoFactorEnabled: false } user)
            {
                return SignInFlow.NoPendingSignIn();
            }

            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);
            }

            return new AuthenticatorSetupResponse(key!, AuthenticatorQrCode.FormatKey(key!), AuthenticatorQrCode.RenderSvg(AuthenticatorQrCode.BuildUri(user.Email!, key!)));
        }
    }

    /// <summary>
    /// Verifies the code (wrong codes count toward the lockout), enables the second factor and signs in.
    /// </summary>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="flow">The shared sign-in steps.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="httpContextAccessor">The current request (ends the password step cookie).</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        SignInFlow flow,
        IAuditLogger audit,
        IHttpContextAccessor httpContextAccessor,
        TimeProvider timeProvider) : IRequestHandler<Command, Result<RecoveryCodesResponse>>
    {
        /// <inheritdoc />
        public async Task<Result<RecoveryCodesResponse>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (await flow.PendingUserAsync() is not { TwoFactorEnabled: false } user)
            {
                return SignInFlow.NoPendingSignIn();
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                return SignInFlow.LockedOut();
            }

            if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, SignInFlow.NormalizeCode(request.Code)))
            {
                await userManager.AccessFailedAsync(user);
                if (await userManager.IsLockedOutAsync(user))
                {
                    await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, "Підключення автентифікатора", cancellationToken);
                    return SignInFlow.LockedOut();
                }

                return Error.Validation(Texts.T("Код неправильний. Перевірте час на телефоні та спробуйте наступний код."));
            }

            if (!await signInManager.CanSignInAsync(user))
            {
                return SignInFlow.NoPendingSignIn();
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);
            var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? [];
            user.RecordSignIn(timeProvider.GetUtcNow());
            await userManager.UpdateAsync(user);

            await httpContextAccessor.HttpContext!.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            await signInManager.SignInWithClaimsAsync(user, request.RememberMe, [new Claim(AppClaimTypes.AuthenticationMethod, "mfa")]);

            await audit.WriteAsync(AuditActions.AuthenticatorEnrolled, user.Id, user.FullName, null, cancellationToken);
            await audit.WriteAsync(AuditActions.SignIn, user.Id, user.FullName, "Пароль + новий автентифікатор", cancellationToken);
            return new RecoveryCodesResponse([.. codes]);
        }
    }

    /// <summary>
    /// <c>GET</c> and <c>POST /api/v1/auth/login/authenticator</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var auth = AuthApi.MapAuth(api);
            auth.MapGet("login/authenticator", KeyAsync).AllowAnonymous();
            auth.MapPost("login/authenticator", HandleAsync).AllowAnonymous();
        }

        /// <summary>
        /// Creates the authenticator key of the pending user (once) with its QR code.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The key.</returns>
        private static async Task<Results<Ok<AuthenticatorSetupResponse>, ProblemHttpResult>> KeyAsync(IRequestHandler<KeyQuery, Result<AuthenticatorSetupResponse>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new KeyQuery(), cancellationToken)).ToOk();

        /// <summary>
        /// Verifies the first authenticator code, enables two-factor authentication, issues recovery codes and signs in.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The recovery codes, shown once.</returns>
        private static async Task<Results<Ok<RecoveryCodesResponse>, ProblemHttpResult>> HandleAsync(CodeRequest code, IRequestHandler<Command, Result<RecoveryCodesResponse>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(code.Code, code.RememberMe), cancellationToken)).ToOk();
    }
}
