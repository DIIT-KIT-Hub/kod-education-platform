using ODC.Web.Features.Notifications.Domain;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// What to tell the recipients of a notification.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="ActorId">Who did it (never notified about their own action).</param>
/// <param name="EntityId">The document, article, chat or request concerned.</param>
/// <param name="Subject">The title of what is concerned.</param>
/// <param name="Preview">The start of the text.</param>
/// <param name="Link">The address of the page to open.</param>
public sealed record NotificationDraft(NotificationKind Kind, Guid? ActorId, Guid EntityId, string Subject, string? Preview, string Link)
{
    /// <summary>
    /// Gets a machine value the client shows translated (the new state of a request).
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets a value indicating whether events collect in the unread notification of the same kind about the same thing
    /// (messages of a chat) instead of adding one each.
    /// </summary>
    public bool Collect { get; init; }

    /// <summary>
    /// Gets the moment from which a recipient is told only once (a document to sign: once per sending for signing).
    /// </summary>
    public DateTimeOffset? OnceSince { get; init; }
}
