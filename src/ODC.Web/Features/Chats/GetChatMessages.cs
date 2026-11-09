using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// One page of messages, older than <c>before</c> or newer than <c>after</c>; the numbers have no gaps, so the client fetches a gap with <c>after</c>.
/// </summary>
public static class GetChatMessages
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="Before">The before.</param>
    /// <param name="After">The after.</param>
    /// <param name="Limit">The limit.</param>
    public sealed record Query(Guid ChatId, long? Before, long? After, int Limit) : IQuery<Result<ChatMessagePage>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, ChatRoom room) : IRequestHandler<Query, Result<ChatMessagePage>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessagePage>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var before = request.Before;
            var after = request.After;
            var limit = request.Limit;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var chat = await (
                    from m in db.ChatMembers
                    where m.ChatId == chatId && m.UserId == me
                    join c in db.Chats on m.ChatId equals c.Id
                    select new { c.LastSequence, m.ClearedSequence })
                .FirstOrDefaultAsync(cancellationToken);
            if (chat is null)
            {
                return ChatRoom.NotFound();
            }

            limit = Math.Clamp(limit, 1, ChatRoom.MaxPageSize);
            var messages = db.ChatMessages.Where(m => m.ChatId == chatId && m.Sequence > chat.ClearedSequence);
            List<ChatMessageDto> page;
            if (before is null && after is { } from)
            {
                page = await room.LoadAsync(messages.Where(m => m.Sequence > from).OrderBy(static m => m.Sequence).Take(limit + 1), me, cancellationToken);
            }
            else
            {
                var upTo = before ?? long.MaxValue;
                page = await room.LoadAsync(messages.Where(m => m.Sequence < upTo).OrderByDescending(static m => m.Sequence).Take(limit + 1), me, cancellationToken);
            }

            var hasMore = page.Count > limit;
            var ordered = page.OrderBy(static m => m.Sequence).ToList();
            if (hasMore)
            {
                // The extra message only tells that more exist: drop it at the far end of the requested direction.
                ordered.RemoveAt(before is null && after is not null ? ordered.Count - 1 : 0);
            }

            return new ChatMessagePage(ordered, hasMore, chat.LastSequence);
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
            ChatRoom.MapChats(api).MapGet("{id:guid}/messages", HandleAsync);
        }

        /// <summary>
        /// Gets one page of messages.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="before">The newest number to stop before (older messages).</param>
        /// <param name="after">The number to continue after (newer messages).</param>
        /// <param name="limit">The page size.</param>
        /// <returns>The page or the problem.</returns>
        private static async Task<Results<Ok<ChatMessagePage>, ProblemHttpResult>> HandleAsync(
            Guid id,
            IRequestHandler<Query, Result<ChatMessagePage>> handler,
            CancellationToken cancellationToken,
            long? before = null,
            long? after = null,
            int limit = ChatRoom.PageSize)
            => (await handler.HandleAsync(new Query(id, before, after, limit), cancellationToken)).ToOk();
    }
}
