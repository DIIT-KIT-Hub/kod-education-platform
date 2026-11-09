using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Edits an own message (a forwarded one cannot be edited).
/// </summary>
public static class EditChatMessage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="MessageId">The message.</param>
    /// <param name="Body">The body.</param>
    public sealed record Command(Guid ChatId, Guid MessageId, string? Body) : ICommand<Result<ChatMessageDto>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="sanitizer">Cleans the HTML of messages.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, IRichTextSanitizer sanitizer, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<ChatMessageDto>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessageDto>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var messageId = request.MessageId;
            var body = request.Body;
            var found = await room.OwnMessageAsync(chatId, messageId, Texts.T("Редагувати можна лише власні повідомлення."), cancellationToken);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            try
            {
                var html = sanitizer.SanitizeMessage(body ?? string.Empty);
                found.Value.Edit(html, sanitizer.ToText(html), timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
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
            ChatRoom.MapChats(api).MapPut("{id:guid}/messages/{messageId:guid}", HandleAsync);
        }

        /// <summary>
        /// Edits an own message.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="messageId">The message.</param>
        /// <param name="message">The new text.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The changed message.</returns>
        private static async Task<Results<Ok<ChatMessageDto>, ProblemHttpResult>> HandleAsync(
            Guid id,
            Guid messageId,
            ChatMessageRequest message,
            IRequestHandler<Command, Result<ChatMessageDto>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, messageId, message.Body), cancellationToken)).ToOk();
    }
}
