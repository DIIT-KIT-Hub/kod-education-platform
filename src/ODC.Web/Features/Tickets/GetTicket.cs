using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Gets a request with its history (its author or a handler) and marks the notifications about it read.
/// </summary>
public static class GetTicket
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The ticket.</param>
    public sealed record Query(Guid Id) : IQuery<Result<TicketDetails>>;

    /// <summary>
    /// Reads the request; handlers also see the author's position and e-mail.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="domainEvents">Raises what the user has seen (their notifications about it are done).</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, TicketDesk desk, DomainEvents domainEvents) : IRequestHandler<Query, Result<TicketDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<TicketDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            if (currentUser.Id is not { } me)
            {
                return TicketDesk.NotFound();
            }

            var handler = desk.IsHandler;
            var row = await db.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id && (handler || t.AuthorId == me))
                .Select(static t => new
                {
                    t.Id,
                    t.Number,
                    t.Subject,
                    t.Category,
                    t.Priority,
                    t.Status,
                    t.AuthorId,
                    Author = new UserRef(t.AuthorId, t.Author!.LastName + " " + t.Author.FirstName + (t.Author.MiddleName != null ? " " + t.Author.MiddleName : string.Empty), t.Author.PhotoId),
                    t.Author.Email,
                    Title = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                    RoleName = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                    UnitName = t.Author.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault(),
                    Assignee = t.AssigneeId == null
                        ? null
                        : new UserRef(t.Assignee!.Id, t.Assignee.LastName + " " + t.Assignee.FirstName + (t.Assignee.MiddleName != null ? " " + t.Assignee.MiddleName : string.Empty), t.Assignee.PhotoId),
                    t.CreatedAt,
                    t.LastActivityAt,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (row is null)
            {
                return TicketDesk.NotFound();
            }

            var messages = await desk.LoadMessagesAsync(row.Id, row.AuthorId, cancellationToken);
            domainEvents.Raise(new ItemSeen(me, id));

            var isMine = row.AuthorId == me;
            var open = row.Status != TicketStatus.Closed;
            return new TicketDetails(
                row.Id,
                row.Number,
                row.Subject,
                row.Category,
                row.Priority,
                row.Status,
                row.Author,
                handler ? StoredText.PositionInUnit(row.Title, row.RoleName, row.UnitName) : null,
                handler ? row.Email : null,
                row.Assignee,
                row.CreatedAt,
                row.LastActivityAt,
                messages,
                isMine,
                handler,
                open,
                open && (handler || isMine));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/tickets/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => TicketDesk.MapTickets(api).MapGet("{id:guid}", HandleAsync);

        /// <summary>
        /// Gets a request with its history.
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The request with its history.</returns>
        private static async Task<Results<Ok<TicketDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<TicketDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
