using ODC.Application.Common.Paging;

namespace ODC.Application.Features.Notifications;

/// <summary>
/// Paging and filter of the notification list (the newest first).
/// </summary>
public sealed record NotificationQuery : PageRequest
{
    /// <summary>
    /// Gets a value indicating whether only unread notifications are listed.
    /// </summary>
    public bool UnreadOnly { get; init; }
}
