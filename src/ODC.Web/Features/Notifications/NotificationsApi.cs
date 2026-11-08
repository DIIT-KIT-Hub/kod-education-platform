namespace ODC.Web.Features.Notifications;

/// <summary>
/// The route group of the notification endpoints. The notifications belong to the current user (replies in news,
/// documents to sign, chats, requests); new ones and changes of the unread count also reach the open pages through the
/// SignalR hub.
/// </summary>
internal static class NotificationsApi
{
    /// <summary>
    /// Maps the group <c>/api/v1/notifications</c>.
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The notification group.</returns>
    public static RouteGroupBuilder MapNotifications(this IEndpointRouteBuilder api) => api.MapGroup("notifications").WithTags("Notifications");
}
