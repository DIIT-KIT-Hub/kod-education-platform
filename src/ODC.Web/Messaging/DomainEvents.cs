using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// The events a handler raises itself, for what happened without an entity change (a user opened a document) or when the
/// handler knows more than the entity (a batch of chat messages). Scoped: delivered with the entity events after the
/// request succeeded.
/// </summary>
public sealed class DomainEvents : IDomainEventSource
{
    /// <summary>
    /// The raised events.
    /// </summary>
    private readonly List<IDomainEvent> _raised = [];

    /// <summary>
    /// Raises an event.
    /// </summary>
    /// <param name="domainEvent">The event.</param>
    public void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _raised.Add(domainEvent);
    }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
    {
        if (_raised.Count == 0)
        {
            return [];
        }

        var events = _raised.ToArray();
        _raised.Clear();
        return events;
    }
}
