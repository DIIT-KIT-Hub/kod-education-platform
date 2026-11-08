using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// Sets the first password and starts the usual sign-in: the next step is the authenticator (enrolled now, or its code
/// when one was enrolled before a password reset).
/// </summary>
public static class SetActivationPassword
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Flow">The activation of this browser.</param>
    /// <param name="NewPassword">The password (Identity validators, breached-password check).</param>
    public sealed record Command(ActivationFlow Flow, string? NewPassword) : ICommand<Result<User>>;

    /// <summary>
    /// Saves the password (the account no longer awaits activation).
    /// </summary>
    /// <param name="activation">The activation engine.</param>
    internal sealed class Handler(AccountActivationService activation) : IRequestHandler<Command, Result<User>>
    {
        /// <inheritdoc />
        public Task<Result<User>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return activation.CompleteAsync(request.Flow, request.NewPassword, cancellationToken);
        }
    }

    /// <summary>
    /// <c>POST /api/v1/auth/activation/password</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ActivationApi.MapActivation(api).MapPost("password", HandleAsync);

        /// <summary>
        /// Sets the first password and starts the sign-in.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <param name="activation">The activation engine (reads the cookie).</param>
        /// <param name="handler">The handler.</param>
        /// <param name="signInManager">The sign-in manager (the password step of the usual sign-in).</param>
        /// <param name="context">The HTTP context (the activation cookie).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next sign-in step.</returns>
        private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> HandleAsync(
            NewPasswordRequest password,
            AccountActivationService activation,
            IRequestHandler<Command, Result<User>> handler,
            SignInManager<User> signInManager,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            if (ActivationApi.CurrentFlow(context.Request, activation) is not { } flow)
            {
                return ActivationApi.NoActivation().ToProblem();
            }

            var result = await handler.HandleAsync(new Command(flow, password.NewPassword), cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.ToProblem();
            }

            ActivationApi.DeleteCookie(context.Response);
            var user = result.Value;

            // The password step of the usual sign-in: it opens the second factor step (mandatory for everyone).
            var signIn = await signInManager.PasswordSignInAsync(user, password.NewPassword ?? string.Empty, isPersistent: false, lockoutOnFailure: true);
            if (!signIn.RequiresTwoFactor && !signIn.Succeeded)
            {
                return Error.Validation(Texts.T("Пароль збережено, але увійти зараз не вдалося. Увійдіть зі сторінки входу.")).ToProblem();
            }

            return TypedResults.Ok(new LoginResponse(user.TwoFactorEnabled ? LoginResponse.TwoFactor : LoginResponse.Setup));
        }
    }
}
