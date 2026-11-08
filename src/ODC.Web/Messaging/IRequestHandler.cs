namespace ODC.Web.Messaging;

/// <summary>
/// Handles one kind of request. Callers (endpoints, other handlers, background jobs) take this interface from dependency
/// injection: it resolves to the handler wrapped in the pipeline (logging, permission check), see
/// <see cref="MessagingServiceCollectionExtensions"/>. There is no central dispatcher.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
/// <typeparam name="TResponse">The answer.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answer.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
