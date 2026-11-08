using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// Holds domain events raised during a request: the database context (events of the tracked entities) and
/// <see cref="DomainEvents"/> (events raised by handlers).
/// </summary>
public interface IDomainEventSource
{
    /// <summary>
    /// Takes the pending events (each is delivered once).
    /// </summary>
    /// <returns>The events, oldest first.</returns>
    IReadOnlyList<IDomainEvent> TakeDomainEvents();
}
