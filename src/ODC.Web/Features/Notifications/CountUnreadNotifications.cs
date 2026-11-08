using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Counts the unread notifications of the current user (the badge).
/// </summary>
public static class CountUnreadNotifications
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Query : IQuery<int>;

    /// <summary>
    /// Counts.
    /// </summary>
    /// <param name="inbox">The user's notifications.</param>
    internal sealed class Handler(NotificationInbox inbox) : IRequestHandler<Query, int>
    {
        /// <inheritdoc />
        public Task<int> HandleAsync(Query request, CancellationToken cancellationToken) => inbox.CountUnreadAsync(cancellationToken);
    }

    /// <summary>
    /// <c>GET /api/v1/notifications/unread-count</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapGet("unread-count", HandleAsync);

        /// <summary>
        /// Counts the unread notifications (the badge).
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number.</returns>
        private static async Task<Ok<UnreadCountResponse>> HandleAsync(IRequestHandler<Query, int> handler, CancellationToken cancellationToken)
            => TypedResults.Ok(new UnreadCountResponse(await handler.HandleAsync(new Query(), cancellationToken)));
    }
}
