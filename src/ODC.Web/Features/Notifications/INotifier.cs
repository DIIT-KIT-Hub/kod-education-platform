namespace ODC.Web.Features.Notifications;

/// <summary>
/// Creates the notifications of other features (news replies, documents to sign, chats, requests) after their change was
/// saved and delivers them to the open pages. A failure is logged and never fails the change that caused it.
/// </summary>
internal interface INotifier
{
    /// <summary>
    /// Notifies people (the actor and inactive accounts are skipped).
    /// </summary>
    /// <param name="draft">What to tell.</param>
    /// <param name="recipients">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task NotifyAsync(NotificationDraft draft, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies people wherever they work: each in their home organization (about something of the master database,
    /// such as a request to the platform support).
    /// </summary>
    /// <param name="draft">What to tell.</param>
    /// <param name="recipients">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task NotifyWhereTheyWorkAsync(NotificationDraft draft, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the unread notifications of a user about something read (they opened the chat or the request), in all
    /// their organizations.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="entityId">The document, article, chat or request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task MarkReadAsync(Guid userId, Guid entityId, CancellationToken cancellationToken);
}
