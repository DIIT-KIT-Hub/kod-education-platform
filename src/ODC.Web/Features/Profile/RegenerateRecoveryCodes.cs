using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Auth;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Profile;

/// <summary>
/// Replaces the recovery codes after the authenticator code is confirmed: a valid session alone is not enough (step-up).
/// </summary>
public static class RegenerateRecoveryCodes
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="AuthenticatorCode">The current code of the authenticator app.</param>
    public sealed record Command(string AuthenticatorCode) : ICommand<Result<IReadOnlyList<string>>>;

    /// <summary>
    /// Verifies the code (single use) and generates new codes.
    /// </summary>
    /// <param name="account">The current user's account.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ProfileAccount account, IIdentityService identity, IAuditLogger audit) : IRequestHandler<Command, Result<IReadOnlyList<string>>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<string>>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await account.LoadWithPositionsAsync(cancellationToken);
            if (user is null)
            {
                return Error.Unauthorized();
            }

            if (!user.TwoFactorEnabled)
            {
                return Error.Validation(Texts.T("Спочатку підключіть застосунок-автентифікатор."));
            }

            if (!await identity.VerifyAuthenticatorCodeAsync(user, request.AuthenticatorCode))
            {
                await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний код підтвердження (нові резервні коди)", cancellationToken);
                return Error.Validation(Texts.T("Код неправильний або вже використаний. Введіть наступний код із застосунку."));
            }

            var codes = await identity.GenerateRecoveryCodesAsync(user);
            await audit.WriteAsync(AuditActions.RecoveryCodesGenerated, user.Id, user.FullName, null, cancellationToken);
            return Result<IReadOnlyList<string>>.Success(codes);
        }
    }

    /// <summary>
    /// <c>POST /api/v1/profile/recovery-codes</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ProfileAccount.MapProfile(api).MapPost("recovery-codes", HandleAsync);

        /// <summary>
        /// Replaces the recovery codes after the authenticator code is confirmed: a valid session alone is not enough.
        /// </summary>
        /// <param name="stepUp">The authenticator code.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The new codes (shown once).</returns>
        private static async Task<Results<Ok<RecoveryCodesResponse>, ProblemHttpResult>> HandleAsync(
            StepUpRequest stepUp,
            IRequestHandler<Command, Result<IReadOnlyList<string>>> handler,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(stepUp.Code), cancellationToken);
            return result.IsSuccess ? TypedResults.Ok(new RecoveryCodesResponse(result.Value)) : result.Error.ToProblem();
        }
    }
}
