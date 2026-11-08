using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Sets the current user's reaction to a message; the same reaction again removes it.
/// </summary>
public static class ReactToChatMessage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="MessageId">The message.</param>
    /// <param name="Kind">The kind.</param>
    public sealed record Command(Guid ChatId, Guid MessageId, ReactionKind Kind) : ICommand<Result<ChatMessageDto>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<ChatMessageDto>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessageDto>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var messageId = request.MessageId;
            var kind = request.Kind;
            if (room.Me is not { } me || !(await room.MemberIdsAsync(chatId, cancellationToken)).Contains(me))
            {
                return ChatRoom.NotFound();
            }

            if (!Enum.IsDefined(kind)
                || !await db.ChatMessages.AnyAsync(m => m.Id == messageId && m.ChatId == chatId && m.Kind == ChatMessageKind.Text && !m.IsDeleted, cancellationToken))
            {
                return Error.NotFound(Texts.T("Повідомлення не знайдено."));
            }

            var existing = await db.ChatReactions.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == me, cancellationToken);
            if (existing is null)
            {
                db.ChatReactions.Add(new ChatReaction(messageId, me, kind, timeProvider.GetUtcNow()));
            }
            else if (existing.Kind == kind)
            {
                db.ChatReactions.Remove(existing);
            }
            else
            {
                existing.Change(kind, timeProvider.GetUtcNow());
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A double click from two tabs: the other request has already saved the reaction.
                db.DiscardChanges();
            }

            return await room.PublishChangedAsync(chatId, messageId, cancellationToken);
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
            ChatRoom.MapChats(api).MapPost("{id:guid}/messages/{messageId:guid}/reactions/{kind}", HandleAsync);
        }

        /// <summary>
        /// Sets the current user's reaction to a message.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="messageId">The message.</param>
        /// <param name="kind">The reaction.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The changed message.</returns>
        private static async Task<Results<Ok<ChatMessageDto>, ProblemHttpResult>> HandleAsync(
            Guid id,
            Guid messageId,
            ReactionKind kind,
            IRequestHandler<Command, Result<ChatMessageDto>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, messageId, kind), cancellationToken)).ToOk();
    }
}
