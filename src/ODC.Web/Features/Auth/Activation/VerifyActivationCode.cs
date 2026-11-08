using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// Checks the code from the e-mail (constant time, single use, at most five wrong codes).
/// </summary>
public static class VerifyActivationCode
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Flow">The activation of this browser.</param>
    /// <param name="Code">The code.</param>
    public sealed record Command(ActivationFlow Flow, string? Code) : ICommand<Result<ActivatedAccount>>;

    /// <summary>
    /// Verifies the code.
    /// </summary>
    /// <param name="activation">The activation engine.</param>
    internal sealed class Handler(AccountActivationService activation) : IRequestHandler<Command, Result<ActivatedAccount>>
    {
        /// <inheritdoc />
        public Task<Result<ActivatedAccount>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return activation.VerifyAsync(request.Flow, request.Code, cancellationToken);
        }
    }

    /// <summary>
    /// <c>POST /api/v1/auth/activation/code</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ActivationApi.MapActivation(api).MapPost("code", HandleAsync);

        /// <summary>
        /// Checks the code from the e-mail.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="activation">The activation engine (reads the cookie).</param>
        /// <param name="handler">The handler.</param>
        /// <param name="request">The HTTP request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>What the next steps need to know.</returns>
        private static async Task<Results<Ok<ActivationVerifiedResponse>, ProblemHttpResult>> HandleAsync(
            ActivationCodeRequest code,
            AccountActivationService activation,
            IRequestHandler<Command, Result<ActivatedAccount>> handler,
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            if (ActivationApi.CurrentFlow(request, activation) is not { } flow)
            {
                return ActivationApi.NoActivation().ToProblem();
            }

            var result = await handler.HandleAsync(new Command(flow, code.Code), cancellationToken);
            return result.IsSuccess
                ? TypedResults.Ok(new ActivationVerifiedResponse(result.Value.FirstName, result.Value.HasAuthenticator, result.Value.HasPhoto))
                : result.Error.ToProblem();
        }
    }
}
