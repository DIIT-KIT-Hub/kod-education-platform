using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Deletes a notification of the current user.
/// </summary>
public static class DeleteNotification
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The notification.</param>
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Deletes it.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="inbox">The user's notifications.</param>
    internal sealed class Handler(ApplicationDbContext db, NotificationInbox inbox) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var notification = await inbox.FindOwnAsync(request.Id, cancellationToken);
            if (notification is null)
            {
                return NotificationInbox.NotFound();
            }

            db.Notifications.Remove(notification);
            await inbox.SaveAsync(cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/notifications/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapDelete("{id:guid}", HandleAsync);

        /// <summary>
        /// Deletes a notification.
        /// </summary>
        /// <param name="id">The notification.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
