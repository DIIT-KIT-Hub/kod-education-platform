using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Features.Notifications.Reactions;

/// <summary>
/// Tells the people of a request about it: the handlers about a new one, the other side about a message or a state
/// change, a handler about a request assigned to them.
/// </summary>
/// <param name="master">The master database (the requests).</param>
/// <param name="db">The organization's database (who handles requests).</param>
/// <param name="catalog">The shared catalogs (roles).</param>
/// <param name="notifier">Creates the notifications.</param>
internal sealed class TicketNotifications(MasterDbContext master, OrganizationDbContext db, Catalog catalog, INotifier notifier) :
    IDomainEventHandler<TicketSubmitted>,
    IDomainEventHandler<TicketCommented>,
    IDomainEventHandler<TicketStatusChanged>,
    IDomainEventHandler<TicketAssigned>
{
    /// <inheritdoc />
    public async Task HandleAsync(TicketSubmitted domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var ticket = await master.Tickets.FirstAsync(t => t.Id == domainEvent.TicketId, cancellationToken);
        await notifier.NotifyAsync(Draft(NotificationKind.TicketCreated, domainEvent.AuthorId, ticket, domainEvent.Description), await HandlersAsync(cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(TicketCommented domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var ticket = await master.Tickets.FirstAsync(t => t.Id == domainEvent.TicketId, cancellationToken);
        var draft = Draft(NotificationKind.TicketReply, domainEvent.AuthorId, ticket, domainEvent.Preview);
        await notifier.NotifyAsync(draft, await CounterpartsAsync(ticket, domainEvent.AuthorId, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(TicketStatusChanged domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var ticket = await master.Tickets.FirstAsync(t => t.Id == domainEvent.TicketId, cancellationToken);
        var draft = Draft(NotificationKind.TicketStatusChanged, domainEvent.ActorId, ticket, null) with { Code = domainEvent.Status.ToString() };
        await notifier.NotifyAsync(draft, await CounterpartsAsync(ticket, domainEvent.ActorId, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(TicketAssigned domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var ticket = await master.Tickets.FirstAsync(t => t.Id == domainEvent.TicketId, cancellationToken);
        await notifier.NotifyAsync(Draft(NotificationKind.TicketAssigned, domainEvent.ActorId, ticket, null), [domainEvent.AssigneeId], cancellationToken);
    }

    /// <summary>
    /// Builds a notification about a request.
    /// </summary>
    /// <param name="kind">What happened.</param>
    /// <param name="actorId">Who did it.</param>
    /// <param name="ticket">The request.</param>
    /// <param name="preview">The start of the text.</param>
    /// <returns>The draft.</returns>
    private static NotificationDraft Draft(NotificationKind kind, Guid actorId, Ticket ticket, string? preview)
        => new(kind, actorId, ticket.Id, $"№ {ticket.Number} · {ticket.Subject}", preview, $"/requests/{ticket.Id}");

    /// <summary>
    /// Finds who should hear about a message or a change (every handler while nobody has the request).
    /// </summary>
    /// <param name="ticket">The request.</param>
    /// <param name="actorId">Who acted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    private async Task<IReadOnlyCollection<Guid>> CounterpartsAsync(Ticket ticket, Guid actorId, CancellationToken cancellationToken)
        => ticket.CounterpartsOf(actorId) ?? await HandlersAsync(cancellationToken);

    /// <summary>
    /// Lists everyone who handles requests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people.</returns>
    private async Task<IReadOnlyCollection<Guid>> HandlersAsync(CancellationToken cancellationToken)
        => await db.MemberIdsWithAsync(catalog, Permissions.Tickets.Manage, cancellationToken);
}
