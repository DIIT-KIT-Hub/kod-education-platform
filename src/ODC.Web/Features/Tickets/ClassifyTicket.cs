using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Changes the category and the priority of a request.
/// </summary>
public static class ClassifyTicket
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="Category">The kind of request.</param>
    /// <param name="Priority">The urgency.</param>
    [RequirePermission(Permissions.Tickets.Manage)]
    public sealed record Command(Guid Id, TicketCategory Category, TicketPriority Priority) : ICommand<Result>;

    /// <summary>
    /// Saves the classification when it changed.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (ticket is null)
            {
                return TicketDesk.NotFound();
            }

            try
            {
                if (ticket.Classify(request.Category, request.Priority))
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                return Result.Success();
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
            }
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/tickets/{id}/classification</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => TicketDesk.MapTickets(api).MapPut("{id:guid}/classification", HandleAsync).RequirePermission(Permissions.Tickets.Manage);

        /// <summary>
        /// Changes the category and the priority of a request.
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="classification">The category and priority.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            TicketClassificationRequest classification,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, classification.Category, classification.Priority), cancellationToken)).ToNoContent();
    }
}
