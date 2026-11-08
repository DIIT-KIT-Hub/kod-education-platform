namespace ODC.Web.Shared.Kernel;

/// <summary>
/// Base type for every domain entity identified by a time-ordered <see cref="Guid"/>.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// The events raised since they were last taken (a field without a property, so EF Core never maps it).
    /// </summary>
    private List<IDomainEvent>? _raised;

    /// <summary>
    /// Gets the unique identifier of the entity (UUID v7, index friendly).
    /// </summary>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    /// <summary>
    /// Takes the raised events (each is delivered once).
    /// </summary>
    /// <returns>The events, oldest first.</returns>
    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
    {
        if (_raised is not { Count: > 0 } events)
        {
            return [];
        }

        _raised = null;
        return events;
    }

    /// <summary>
    /// Raises an event, delivered after the request that changed the entity succeeded.
    /// </summary>
    /// <param name="domainEvent">The event.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        (_raised ??= []).Add(domainEvent);
    }
}
