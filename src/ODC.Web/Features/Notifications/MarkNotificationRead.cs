using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Notifications;

/// <summary>
/// Marks a notification of the current user read.
/// </summary>
public static class MarkNotificationRead
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The notification.</param>
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Marks it.
    /// </summary>
    /// <param name="inbox">The user's notifications.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(NotificationInbox inbox, TimeProvider timeProvider) : IRequestHandler<Command, Result>
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

            if (notification.MarkRead(timeProvider.GetUtcNow()))
            {
                await inbox.SaveAsync(cancellationToken);
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/notifications/{id}/read</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapNotifications().MapPut("{id:guid}/read", HandleAsync);

        /// <summary>
        /// Marks a notification read.
        /// </summary>
        /// <param name="id">The notification.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
