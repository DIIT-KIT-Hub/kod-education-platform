using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications.Reactions;

/// <summary>
/// Marks the unread notifications about something read when the user has seen it (opened the document or the request,
/// read the chat to the end) or it is gone for them.
/// </summary>
/// <param name="notifier">Marks the notifications.</param>
internal sealed class SeenItems(INotifier notifier) : IDomainEventHandler<ItemSeen>
{
    /// <inheritdoc />
    public Task HandleAsync(ItemSeen domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return notifier.MarkReadAsync(domainEvent.UserId, domainEvent.ItemId, cancellationToken);
    }
}
