using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Assigns a request to a handler or releases it.
/// </summary>
public static class AssignTicket
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="AssigneeId">The handler, or <see langword="null"/> to release the request.</param>
    [RequirePermission(Permissions.Tickets.Manage)]
    public sealed record Command(Guid Id, Guid? AssigneeId) : ICommand<Result>;

    /// <summary>
    /// Assigns and tells the new handler.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(MasterDbContext db, ICurrentUser currentUser, TicketDesk desk, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Forbidden();
            }

            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (ticket is null)
            {
                return TicketDesk.NotFound();
            }

            string? name = null;
            if (request.AssigneeId is { } assignee)
            {
                if (!(await desk.HandlerIdsAsync(cancellationToken)).Contains(assignee))
                {
                    return Error.Validation(Texts.T("Звернення можна призначити лише тому, хто опрацьовує звернення."));
                }

                name = await desk.NameOfAsync(assignee, cancellationToken);
            }

            var note = ticket.Assign(me, request.AssigneeId, name, timeProvider.GetUtcNow());
            if (note is null)
            {
                return Result.Success();
            }

            db.TicketMessages.Add(note);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/tickets/{id}/assignee</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => TicketDesk.MapTickets(api).MapPut("{id:guid}/assignee", HandleAsync).RequirePermission(Permissions.Tickets.Manage);

        /// <summary>
        /// Assigns a request to a handler or releases it.
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="assignee">The handler.</param>
        /// <param name="handler">The handler of the command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            TicketAssigneeRequest assignee,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, assignee.AssigneeId), cancellationToken)).ToNoContent();
    }
}
