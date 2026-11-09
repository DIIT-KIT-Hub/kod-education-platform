using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;

namespace ODC.Web.Features.Notifications.Reactions;

/// <summary>
/// Tells the author of a comment about a reply to it (never about their own reply).
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="notifier">Creates the notifications.</param>
internal sealed class NewsNotifications(OrganizationDbContext db, INotifier notifier) : IDomainEventHandler<NewsCommentPosted>
{
    /// <inheritdoc />
    public async Task HandleAsync(NewsCommentPosted domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent.ParentAuthorId is not { } parentAuthor || parentAuthor == domainEvent.AuthorId)
        {
            return;
        }

        var article = await db.NewsArticles.Where(a => a.Id == domainEvent.ArticleId).Select(static a => new { a.Title, a.Slug }).FirstAsync(cancellationToken);
        var body = await db.NewsComments.Where(c => c.Id == domainEvent.CommentId).Select(static c => c.Body).FirstAsync(cancellationToken);
        var link = $"/news/{article.Slug}#comment-{domainEvent.CommentId}";
        var draft = new NotificationDraft(NotificationKind.NewsReply, domainEvent.AuthorId, domainEvent.ArticleId, article.Title, body, link);
        await notifier.NotifyAsync(draft, [parentAuthor], cancellationToken);
    }
}
