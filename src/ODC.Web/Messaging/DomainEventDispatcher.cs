using System.Collections.Concurrent;
using System.Reflection;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// Delivers domain events to their handlers, one after another in the request's scope (the handlers see the saved
/// state through the same database context). A failing handler is logged and the others still run.
/// </summary>
/// <param name="provider">The request's services.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class DomainEventDispatcher(IServiceProvider provider, ILogger<DomainEventDispatcher> logger)
{
    /// <summary>
    /// The open generic delivery method.
    /// </summary>
    private static readonly MethodInfo DeliverMethod = typeof(DomainEventDispatcher).GetMethod(nameof(DeliverAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// The typed delivery of every event type, built once.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<DomainEventDispatcher, IDomainEvent, CancellationToken, Task>> Deliveries = new();

    /// <summary>
    /// Delivers events.
    /// </summary>
    /// <param name="events">The events, oldest first.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every handler ran.</returns>
    public async Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var domainEvent in events)
        {
            var deliver = Deliveries.GetOrAdd(
                domainEvent.GetType(),
                static type => DeliverMethod.MakeGenericMethod(type).CreateDelegate<Func<DomainEventDispatcher, IDomainEvent, CancellationToken, Task>>());
            await deliver(this, domainEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Logs a failed handler.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="eventName">The event type.</param>
    /// <param name="handler">The handler type.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Handler {Handler} of the domain event {EventName} failed")]
    private static partial void LogHandlerFailed(ILogger logger, string eventName, string handler, Exception exception);

    /// <summary>
    /// Delivers an event to every handler of its type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="domainEvent">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every handler ran.</returns>
    private async Task DeliverAsync<TEvent>(IDomainEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in provider.GetServices<IDomainEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
            {
                LogHandlerFailed(logger, typeof(TEvent).Name, handler.GetType().Name, exception);
            }
        }
    }
}
