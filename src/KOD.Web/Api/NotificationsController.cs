using KOD.Application.Common.Paging;
using KOD.Application.Features.Notifications;
using KOD.Web.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The notifications of the current user (replies in news, documents to sign, chats, requests). New ones and changes of
/// the unread count also reach the open pages through the SignalR hub.
/// </summary>
/// <param name="notifications">The notification service.</param>
[Route("api/v1/notifications")]
public sealed class NotificationsController(INotificationService notifications) : ApiControllerBase
{
    /// <summary>
    /// Lists the notifications, the newest first.
    /// </summary>
    /// <param name="query">The paging and the filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List([FromQuery] NotificationQuery query, CancellationToken cancellationToken)
        => Ok(await notifications.ListAsync(query, cancellationToken));

    /// <summary>
    /// Counts the unread notifications (the badge).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number.</returns>
    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken cancellationToken)
        => Ok(new UnreadCountResponse(await notifications.CountUnreadAsync(cancellationToken)));

    /// <summary>
    /// Marks a notification read.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => FromResult(await notifications.MarkReadAsync(id, cancellationToken));

    /// <summary>
    /// Marks every notification read.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204.</returns>
    [HttpPut("read")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a notification.
    /// </summary>
    /// <param name="id">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await notifications.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Deletes the read notifications.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204.</returns>
    [HttpDelete("read")]
    public async Task<ActionResult> DeleteRead(CancellationToken cancellationToken)
    {
        await notifications.DeleteReadAsync(cancellationToken);
        return NoContent();
    }
}
