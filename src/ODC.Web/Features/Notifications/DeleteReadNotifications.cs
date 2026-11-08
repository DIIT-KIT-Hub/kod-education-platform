using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Deletes the read notifications of the current user.
/// </summary>
public static class DeleteReadNotifications
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Command : ICommand<Result>;

    /// <summary>
    /// Deletes them.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="inbox">The user's notifications.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, NotificationInbox inbox) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.Id is not { } me)
            {
                return Result.Success();
            }

            var read = await db.Notifications.Where(n => n.UserId == me && n.ReadAt != null).ToListAsync(cancellationToken);
            if (read.Count > 0)
            {
                db.Notifications.RemoveRange(read);
                await inbox.SaveAsync(cancellationToken);
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/notifications/read</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapDelete("read", HandleAsync);

        /// <summary>
        /// Deletes the read notifications.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(), cancellationToken)).ToNoContent();
    }
}
