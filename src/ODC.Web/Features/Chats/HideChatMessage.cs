using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Hides any message for the current user only.
/// </summary>
public static class HideChatMessage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="MessageId">The message.</param>
    public sealed record Command(Guid ChatId, Guid MessageId) : ICommand<Result<ChatMessageDto>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="events">Delivers changes to the members' pages.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, IChatEvents events, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<ChatMessageDto>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessageDto>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var messageId = request.MessageId;
            if (room.Me is not { } me || !(await room.MemberIdsAsync(chatId, cancellationToken)).Contains(me))
            {
                return ChatRoom.NotFound();
            }

            var kind = await db.ChatMessages
                .Where(m => m.Id == messageId && m.ChatId == chatId)
                .Select(static m => (ChatMessageKind?)m.Kind)
                .FirstOrDefaultAsync(cancellationToken);
            if (kind is null)
            {
                return Error.NotFound(Texts.T("Повідомлення не знайдено."));
            }

            if (kind != ChatMessageKind.Text)
            {
                return Error.Validation(Texts.T("Це повідомлення не можна видалити."));
            }

            if (!await db.HiddenChatMessages.AnyAsync(h => h.MessageId == messageId && h.UserId == me, cancellationToken))
            {
                db.HiddenChatMessages.Add(new HiddenChatMessage(chatId, messageId, me, timeProvider.GetUtcNow()));
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Another page of the user hid it at the same moment.
                    db.DiscardChanges();
                }
            }

            // The user's other pages replace the message with the placeholder too.
            var placeholder = (await room.LoadAsync(db.ChatMessages.Where(m => m.Id == messageId), me, cancellationToken))[0];
            events.MessageChanged([me], placeholder);
            return placeholder;
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
            ChatRoom.MapChats(api).MapPost("{id:guid}/messages/{messageId:guid}/hide", HandleAsync);
        }

        /// <summary>
        /// Hides a message for the current user.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="messageId">The message.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The placeholder.</returns>
        private static async Task<Results<Ok<ChatMessageDto>, ProblemHttpResult>> HandleAsync(Guid id, Guid messageId, IRequestHandler<Command, Result<ChatMessageDto>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, messageId), cancellationToken)).ToOk();
    }
}
