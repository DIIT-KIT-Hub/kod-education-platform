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
/// The password step of a sign-in. Unknown accounts, wrong passwords and locked-out accounts get the same answer, so
/// the endpoint does not reveal which e-mails exist (OWASP ASVS 2.2.1 / 3.2). The second factor is mandatory: success
/// only opens the two-factor (or enrolment) step.
/// </summary>
public static class SignIn
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Email">The e-mail.</param>
    /// <param name="Password">The password.</param>
    /// <param name="RememberMe">Whether the sign-in survives the browser session.</param>
    public sealed record Command(string? Email, string? Password, bool RememberMe) : ICommand<Result<LoginResponse>>;

    /// <summary>
    /// Verifies the password with lockout.
    /// </summary>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(SignInManager<User> signInManager, UserManager<User> userManager, IAuditLogger audit) : IRequestHandler<Command, Result<LoginResponse>>
    {
        /// <inheritdoc />
        public async Task<Result<LoginResponse>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var email = request.Email?.Trim() ?? string.Empty;
            var user = email.Length == 0 ? null : await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                // Spend comparable time to avoid revealing which e-mails exist.
                _ = userManager.PasswordHasher.HashPassword(new User("timing@odc.local", "-", "-", null), request.Password ?? string.Empty);
                await audit.WriteAsync(AuditActions.SignInFailed, null, email, "Невідомий обліковий запис", cancellationToken);
                return Error.Validation(SignInFlow.GenericError);
            }

            var result = await signInManager.PasswordSignInAsync(user, request.Password ?? string.Empty, request.RememberMe, lockoutOnFailure: true);
            if (result.RequiresTwoFactor || result.Succeeded)
            {
                return new LoginResponse(user.TwoFactorEnabled ? LoginResponse.TwoFactor : LoginResponse.Setup);
            }

            if (result.IsLockedOut)
            {
                // Identity reports a lockout before checking the password: answering differently would reveal the account
                // and let a locked-out attacker keep testing passwords. Hash anyway so the timing matches.
                _ = userManager.PasswordHasher.HashPassword(user, request.Password ?? string.Empty);
                await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, "Спроба входу під час блокування", cancellationToken);
                return Error.Validation(SignInFlow.GenericError);
            }

            if (result.IsNotAllowed)
            {
                // A blocked account: say so only to someone who knows the password; wrong guesses count toward the lockout.
                if (await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
                {
                    await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Обліковий запис заблоковано", cancellationToken);
                    return Error.Validation(Texts.T("Обліковий запис заблоковано. Зверніться до адміністратора."));
                }

                await userManager.AccessFailedAsync(user);
                await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль (заблокований обліковий запис)", cancellationToken);
                return Error.Validation(SignInFlow.GenericError);
            }

            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль", cancellationToken);
            return Error.Validation(SignInFlow.GenericError);
        }
    }

    /// <summary>
    /// <c>POST /api/v1/auth/login</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapPost("login", HandleAsync).AllowAnonymous();

        /// <summary>
        /// Verifies the e-mail and password.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next step.</returns>
        private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> HandleAsync(LoginRequest credentials, IRequestHandler<Command, Result<LoginResponse>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(credentials.Email, credentials.Password, credentials.RememberMe), cancellationToken)).ToOk();
    }
}
