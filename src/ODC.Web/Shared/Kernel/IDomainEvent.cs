namespace ODC.Web.Shared.Kernel;

/// <summary>
/// Something that happened in the domain and that other parts of the cabinet react to (notifications, e-mail). Events
/// are raised by entities (<see cref="Entity.Raise"/>) or by handlers (<c>DomainEvents.Raise</c>) and delivered only
/// after the request that raised them succeeded, so a reaction never sees a change that was not saved.
/// </summary>
#pragma warning disable CA1040 // A marker interface: the dispatcher finds the handlers by the event type.
public interface IDomainEvent;
#pragma warning restore CA1040
