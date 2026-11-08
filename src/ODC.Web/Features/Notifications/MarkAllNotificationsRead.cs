using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Marks every notification of the current user read.
/// </summary>
public static class MarkAllNotificationsRead
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Command : ICommand<Result>;

    /// <summary>
    /// Marks them.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="inbox">The user's notifications.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, NotificationInbox inbox, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.Id is not { } me)
            {
                return Result.Success();
            }

            var unread = await db.Notifications.Where(n => n.UserId == me && n.ReadAt == null).ToListAsync(cancellationToken);
            if (unread.Count > 0)
            {
                var moment = timeProvider.GetUtcNow();
                unread.ForEach(n => n.MarkRead(moment));
                await inbox.SaveAsync(cancellationToken);
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/notifications/read</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapPut("read", HandleAsync);

        /// <summary>
        /// Marks every notification read.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(), cancellationToken)).ToNoContent();
    }
}
