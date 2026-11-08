using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Lists the notifications of the current user, the newest first.
/// </summary>
public static class ListNotifications
{
    /// <summary>
    /// Paging and filter of the notification list.
    /// </summary>
    public sealed record Query : PageRequest, IQuery<PagedResult<NotificationDto>>
    {
        /// <summary>
        /// Gets a value indicating whether only unread notifications are listed.
        /// </summary>
        public bool? UnreadOnly { get; init; }
    }

    /// <summary>
    /// Reads the page.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser) : IRequestHandler<Query, PagedResult<NotificationDto>>
    {
        /// <inheritdoc />
        public async Task<PagedResult<NotificationDto>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return PagedResult<NotificationDto>.Empty(request);
            }

            var notifications = db.Notifications.AsNoTracking().Where(n => n.UserId == me);
            if (request.UnreadOnly == true)
            {
                notifications = notifications.Where(static n => n.ReadAt == null);
            }

            return await notifications
                .OrderByDescending(static n => n.CreatedAt)
                .ThenByDescending(static n => n.Id)
                .Select(NotificationRows.Dto)
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/notifications</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists the notifications, the newest first.
        /// </summary>
        /// <param name="query">The paging and the filter.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Ok<PagedResult<NotificationDto>>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, PagedResult<NotificationDto>> handler,
            CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
    }
}
