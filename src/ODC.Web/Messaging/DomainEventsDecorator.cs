using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// The innermost step of the pipeline: after the handler succeeded (its changes are saved), takes the domain events
/// raised meanwhile and delivers them; events raised by the reactions are delivered too. A failed result or an
/// exception drops the events, so nobody hears about a change that did not happen.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
/// <typeparam name="TResponse">The answer.</typeparam>
/// <param name="inner">The handler.</param>
/// <param name="sources">Where the events wait (the database context and <see cref="DomainEvents"/>).</param>
/// <param name="dispatcher">Delivers the events.</param>
internal sealed class DomainEventsDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IDomainEventSource> sources,
    DomainEventDispatcher dispatcher)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// How many rounds of events raised by reactions are delivered before giving up (a guard against a cycle).
    /// </summary>
    private const int MaxRounds = 4;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        TResponse response;
        try
        {
            response = await inner.HandleAsync(request, cancellationToken);
        }
        catch
        {
            Take();
            throw;
        }

        if (response is IOutcome { Error: not null })
        {
            Take();
            return response;
        }

        for (var round = 0; round < MaxRounds; round++)
        {
            var events = Take();
            if (events.Count == 0)
            {
                break;
            }

            await dispatcher.PublishAsync(events, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Takes the pending events of every source.
    /// </summary>
    /// <returns>The events.</returns>
    private List<IDomainEvent> Take() => [.. sources.SelectMany(static s => s.TakeDomainEvents())];
}
