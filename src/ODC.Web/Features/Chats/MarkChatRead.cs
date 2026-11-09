using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Moves the read position of the current user; read to the end answers the notification about the chat.
/// </summary>
public static class MarkChatRead
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="Sequence">The sequence.</param>
    public sealed record Command(Guid ChatId, long Sequence) : ICommand<Result>;

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
            var sequence = request.Sequence;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
            if (member is null)
            {
                return ChatRoom.NotFound();
            }

            var last = await db.Chats.Where(c => c.Id == chatId).Select(static c => c.LastSequence).FirstAsync(cancellationToken);
            if (member.MarkRead(Math.Min(sequence, last)))
            {
                await db.SaveChangesAsync(cancellationToken);
                events.Read(await room.MemberIdsAsync(chatId, cancellationToken), chatId, me, member.LastReadSequence);
            }

            // Read to the end: the notification about the chat is done.
            if (member.LastReadSequence >= last)
            {
                domainEvents.Raise(new ItemSeen(me, chatId));
            }

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
            ChatRoom.MapChats(api).MapPut("{id:guid}/read", HandleAsync);
        }

        /// <summary>
        /// Moves the read position of the current user.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="read">The last read number.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, ChatReadRequest read, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, read.Sequence), cancellationToken)).ToNoContent();
    }
}
