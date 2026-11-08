using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// Reacts to a domain event after the request that raised it succeeded. A feature reacts to the events of another one
/// without the other knowing about it (notifications about documents, requests, chats and news). A failure is logged
/// and never fails the request: its change is already saved.
/// </summary>
/// <typeparam name="TEvent">The event.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="domainEvent">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when handled.</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
