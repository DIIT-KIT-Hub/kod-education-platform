using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The first step of the activation and the state after a reload: a one-time code goes to the e-mail when it belongs
/// to an account waiting for activation. The answer is the same for every address (no enumeration).
/// </summary>
public static class StartActivation
{
    /// <summary>
    /// Sends a code to the address.
    /// </summary>
    /// <param name="Email">The e-mail.</param>
    public sealed record Command(string? Email) : ICommand<Result<ActivationStart>>;

    /// <summary>
    /// The state of the activation of this browser.
    /// </summary>
    /// <param name="Flow">The activation read from the cookie.</param>
    public sealed record StatusQuery(ActivationFlow Flow) : IQuery<ActivationStatus>;

    /// <summary>
    /// Starts the activation.
    /// </summary>
    /// <param name="activation">The activation engine.</param>
    internal sealed class Handler(AccountActivationService activation) : IRequestHandler<Command, Result<ActivationStart>>
    {
        /// <inheritdoc />
        public Task<Result<ActivationStart>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return activation.StartAsync(request.Email, cancellationToken);
        }
    }

    /// <summary>
    /// Reads the state.
    /// </summary>
    /// <param name="activation">The activation engine.</param>
    internal sealed class StatusHandler(AccountActivationService activation) : IRequestHandler<StatusQuery, ActivationStatus>
    {
        /// <inheritdoc />
        public Task<ActivationStatus> HandleAsync(StatusQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return activation.GetStatusAsync(request.Flow, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET</c>, <c>POST</c> and <c>DELETE /api/v1/auth/activation</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var activation = ActivationApi.MapActivation(api);
            activation.MapGet(string.Empty, StatusAsync);
            activation.MapPost(string.Empty, StartAsync).RequireRateLimiting(ActivationApi.RateLimitPolicy);
            activation.MapDelete(string.Empty, Cancel);
        }

        /// <summary>
        /// Gets the activation of this browser (after a reload of the page).
        /// </summary>
        /// <param name="activation">The activation engine (reads the cookie).</param>
        /// <param name="handler">The handler.</param>
        /// <param name="request">The HTTP request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The state.</returns>
        private static async Task<Results<Ok<ActivationStatusResponse>, ProblemHttpResult>> StatusAsync(
            AccountActivationService activation,
            IRequestHandler<StatusQuery, ActivationStatus> handler,
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            if (ActivationApi.CurrentFlow(request, activation) is not { } flow)
            {
                return ActivationApi.NoActivation().ToProblem();
            }

            return TypedResults.Ok(ActivationApi.ToResponse(await handler.HandleAsync(new StatusQuery(flow), cancellationToken)));
        }

        /// <summary>
        /// Sends a one-time code to the e-mail when it belongs to an account waiting for activation. The answer is the
        /// same for every address, so the endpoint does not reveal which accounts exist.
        /// </summary>
        /// <param name="start">The e-mail.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="response">The HTTP response (the activation cookie).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The state.</returns>
        private static async Task<Results<Ok<ActivationStatusResponse>, ProblemHttpResult>> StartAsync(
            ActivationStartRequest start,
            IRequestHandler<Command, Result<ActivationStart>> handler,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(start.Email), cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error.ToProblem();
            }

            ActivationApi.WriteCookie(response, result.Value.Ticket);
            return TypedResults.Ok(ActivationApi.ToResponse(result.Value.Status));
        }

        /// <summary>
        /// Forgets the activation of this browser (to start over with another e-mail).
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>204.</returns>
        private static NoContent Cancel(HttpResponse response)
        {
            ActivationApi.DeleteCookie(response);
            return TypedResults.NoContent();
        }
    }
}
