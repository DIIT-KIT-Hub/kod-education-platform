using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Changes the state of a request (a handler; the author may close their own).
/// </summary>
public static class ChangeTicketStatus
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="Status">The new state.</param>
    public sealed record Command(Guid Id, TicketStatus Status) : ICommand<Result>;

    /// <summary>
    /// Changes the state (the domain checks who may) and tells the other side.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, TicketDesk desk, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return TicketDesk.NotFound();
            }

            var ticket = await desk.VisibleAsync(request.Id, me, cancellationToken);
            if (ticket is null)
            {
                return TicketDesk.NotFound();
            }

            try
            {
                db.TicketMessages.Add(ticket.ChangeStatus(me, desk.IsHandler, request.Status, timeProvider.GetUtcNow()));
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/tickets/{id}/status</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => TicketDesk.MapTickets(api).MapPut("{id:guid}/status", HandleAsync);

        /// <summary>
        /// Changes the state of a request (a handler; the author may close their own).
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="status">The new state.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            TicketStatusRequest status,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, status.Status), cancellationToken)).ToNoContent();
    }
}
