using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Clears the history for the current user only; a cleared one-to-one chat leaves the list until a newer message.
/// </summary>
public static class ClearChatHistory
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    public sealed record Command(Guid ChatId) : ICommand<Result>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="events">Delivers changes to the members' pages.</param>
    /// <param name="domainEvents">Raises what the user has seen (their notifications about it are done).</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, IChatEvents events, DomainEvents domainEvents, ChatRoom room) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
            if (member is null)
            {
                return ChatRoom.NotFound();
            }

            // A message sent meanwhile takes a newer number and stays visible.
            var last = await db.Chats.Where(c => c.Id == chatId).Select(static c => c.LastSequence).FirstAsync(cancellationToken);
            var read = member.LastReadSequence;
            member.ClearHistory(last);

            // Messages hidden one by one are all behind the cleared position now.
            db.HiddenChatMessages.RemoveRange(await db.HiddenChatMessages.Where(h => h.ChatId == chatId && h.UserId == me).ToListAsync(cancellationToken));
            await db.SaveChangesAsync(cancellationToken);

            events.HistoryCleared([me], chatId);
            if (member.LastReadSequence > read)
            {
                events.Read(await room.MemberIdsAsync(chatId, cancellationToken), chatId, me, member.LastReadSequence);
            }

            domainEvents.Raise(new ItemSeen(me, chatId));
            return Result.Success();
        }
    }

    /// <summary>
    /// The HTTP side of the slice.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            ChatRoom.MapChats(api).MapPost("{id:guid}/clear", HandleAsync);
        }

        /// <summary>
        /// Clears the history for the current user.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
