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
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// The request lists: the current user's own requests, all requests (handlers) and the people who handle them.
/// </summary>
public static class ListTickets
{
    /// <summary>
    /// The current user's requests.
    /// </summary>
    public sealed record MineQuery : TicketQuery, IQuery<PagedResult<TicketListItem>>;

    /// <summary>
    /// All requests (handlers only).
    /// </summary>
    [RequirePermission(Permissions.Tickets.Manage)]
    public sealed record AllQuery : TicketQuery, IQuery<Result<PagedResult<TicketListItem>>>;

    /// <summary>
    /// The people who handle requests (assignee choices).
    /// </summary>
    [RequirePermission(Permissions.Tickets.Manage)]
    public sealed record HandlersQuery : IQuery<Result<IReadOnlyList<UserRef>>>;

    /// <summary>
    /// Reads the current user's requests.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    internal sealed class MineHandler(MasterDbContext db, ICurrentUser currentUser, TicketDesk desk) : IRequestHandler<MineQuery, PagedResult<TicketListItem>>
    {
        /// <inheritdoc />
        public async Task<PagedResult<TicketListItem>> HandleAsync(MineQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return PagedResult<TicketListItem>.Empty(request);
            }

            return await desk.Project(TicketDesk.Filter(db.Tickets.AsNoTracking().Where(t => t.AuthorId == me), request, me)).ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Reads all requests.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    internal sealed class AllHandler(MasterDbContext db, ICurrentUser currentUser, TicketDesk desk) : IRequestHandler<AllQuery, Result<PagedResult<TicketListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<TicketListItem>>> HandleAsync(AllQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Forbidden();
            }

            return await desk.Project(TicketDesk.Filter(db.Tickets.AsNoTracking(), request, me)).ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Reads the handlers (a system administrator always holds every permission).
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared request rules (who handles requests).</param>
    internal sealed class HandlersHandler(MasterDbContext db, TicketDesk desk) : IRequestHandler<HandlersQuery, Result<IReadOnlyList<UserRef>>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<UserRef>>> HandleAsync(HandlersQuery request, CancellationToken cancellationToken)
        {
            var ids = await desk.HandlerIdsAsync(cancellationToken);
            var handlers = await db.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
                .Select(static u => new UserRef(u.Id, u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty), u.PhotoId))
                .ToListAsync(cancellationToken);
            return handlers;
        }
    }

    /// <summary>
    /// <c>GET /api/v1/tickets/mine</c>, <c>GET /api/v1/tickets</c> and <c>GET /api/v1/tickets/handlers</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var tickets = TicketDesk.MapTickets(api);
            tickets.MapGet("mine", MineAsync);
            tickets.MapGet(string.Empty, AllAsync).RequirePermission(Permissions.Tickets.Manage);
            tickets.MapGet("handlers", HandlersAsync).RequirePermission(Permissions.Tickets.Manage);
        }

        /// <summary>
        /// Lists the requests of the current user.
        /// </summary>
        /// <param name="query">The search, filters, sorting and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Ok<PagedResult<TicketListItem>>> MineAsync(
            [AsParameters] MineQuery query,
            IRequestHandler<MineQuery, PagedResult<TicketListItem>> handler,
            CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));

        /// <summary>
        /// Lists the requests of all users.
        /// </summary>
        /// <param name="query">The search, filters, sorting and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<TicketListItem>>, ProblemHttpResult>> AllAsync(
            [AsParameters] AllQuery query,
            IRequestHandler<AllQuery, Result<PagedResult<TicketListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();

        /// <summary>
        /// Lists the people who handle requests.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The people.</returns>
        private static async Task<Results<Ok<IReadOnlyList<UserRef>>, ProblemHttpResult>> HandlersAsync(
            IRequestHandler<HandlersQuery, Result<IReadOnlyList<UserRef>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new HandlersQuery(), cancellationToken)).ToOk();
    }
}
