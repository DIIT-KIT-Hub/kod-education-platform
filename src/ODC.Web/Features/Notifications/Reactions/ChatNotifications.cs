using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;

namespace ODC.Web.Features.Notifications.Reactions;

/// <summary>
/// Tells the members of a chat about new messages: one unread notification per chat collects them until it is read.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="notifier">Creates the notifications.</param>
internal sealed class ChatNotifications(ApplicationDbContext db, INotifier notifier) : IDomainEventHandler<ChatMessagesPosted>
{
    /// <inheritdoc />
    public async Task HandleAsync(ChatMessagesPosted domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var chatId = domainEvent.ChatId;
        var title = await db.Chats.Where(c => c.Id == chatId).Select(static c => c.Title).FirstOrDefaultAsync(cancellationToken);
        if (title is null)
        {
            // A one-to-one chat is called after the person who writes.
            var author = await db.Users
                .Where(u => u.Id == domainEvent.AuthorId)
                .Select(static u => new { u.LastName, u.FirstName, u.MiddleName })
                .FirstAsync(cancellationToken);
            title = author.MiddleName is null ? $"{author.LastName} {author.FirstName}" : $"{author.LastName} {author.FirstName} {author.MiddleName}";
        }

        var text = await db.ChatMessages.Where(m => m.Id == domainEvent.LastMessageId).Select(static m => m.Text).FirstAsync(cancellationToken);
        var members = await db.ChatMembers.Where(m => m.ChatId == chatId).Select(static m => m.UserId).ToListAsync(cancellationToken);

        var draft = new NotificationDraft(NotificationKind.ChatMessage, domainEvent.AuthorId, chatId, title, text.Length > 0 ? text : null, $"/chats/{chatId}") { Collect = true };
        await notifier.NotifyAsync(draft, members, cancellationToken);
    }
}
