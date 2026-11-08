using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Sends a message with files. The client id makes a resend idempotent; every message takes the next number of the chat.
/// </summary>
public static class PostChatMessage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="Message">The message.</param>
    public sealed record Command(Guid ChatId, SendChatMessage Message) : ICommand<Result<ChatMessageDto>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="sanitizer">Cleans the HTML of messages.</param>
    /// <param name="events">Delivers changes to the members' pages.</param>
    /// <param name="domainEvents">Raises the new messages (the notification of the members).</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, IRichTextSanitizer sanitizer, IChatEvents events, DomainEvents domainEvents, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<ChatMessageDto>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessageDto>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var message = request.Message;
            ArgumentNullException.ThrowIfNull(message);
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            if (message.ClientId == Guid.Empty)
            {
                return Error.Validation(Texts.T("Повідомлення не має ідентифікатора."));
            }

            var members = await room.MemberIdsAsync(chatId, cancellationToken);
            if (!members.Contains(me))
            {
                return ChatRoom.NotFound();
            }

            // A message resent after a lost answer is already there.
            if (await room.FindSentAsync(chatId, me, message.ClientId, cancellationToken) is { } sent)
            {
                return sent;
            }

            var body = sanitizer.SanitizeMessage(message.Body ?? string.Empty);
            var text = sanitizer.ToText(body);
            var stored = new List<string>();
            try
            {
                var attachments = await room.StoreAsync(message.Files, stored, cancellationToken);
                if (!attachments.IsSuccess)
                {
                    await room.DeleteFilesAsync(stored, cancellationToken);
                    return attachments.Error;
                }

                for (var attempt = 1; ; attempt++)
                {
                    var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
                    var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
                    if (member is null)
                    {
                        await room.DeleteFilesAsync(stored, cancellationToken);
                        return ChatRoom.NotFound();
                    }

                    var added = new ChatMessage(chatId, chat.NextSequence(timeProvider.GetUtcNow()), me, message.ClientId, body, text, attachments.Value);
                    db.ChatMessages.Add(added);
                    member.MarkRead(added.Sequence);
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < ChatRoom.SendAttempts)
                    {
                        // Another message took the number: number this one again.
                        db.DiscardChanges();
                        continue;
                    }
                    catch (DbUpdateException exception) when (exception is not DbUpdateConcurrencyException)
                    {
                        // The same message sent twice at once: the other request saved it.
                        db.DiscardChanges();
                        if (await room.FindSentAsync(chatId, me, message.ClientId, cancellationToken) is { } twin)
                        {
                            await room.DeleteFilesAsync(stored, cancellationToken);
                            return twin;
                        }

                        throw;
                    }

                    var dto = (await room.LoadAsync(db.ChatMessages.Where(m => m.Id == added.Id), null, cancellationToken))[0];
                    var recipients = await room.MemberIdsAsync(chatId, cancellationToken);
                    events.MessageAdded(recipients, dto);

                    domainEvents.Raise(new ChatMessagesPosted(chatId, me, added.Id));
                    return dto;
                }
            }
            catch (DomainException exception)
            {
                await room.DeleteFilesAsync(stored, cancellationToken);
                return Error.Validation(exception.Message);
            }
            catch
            {
                await room.DeleteFilesAsync(stored, cancellationToken);
                throw;
            }
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
            ChatRoom.MapChats(api).MapPost("{id:guid}/messages", HandleAsync).WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxChatMessageSize + (1024 * 1024)));
        }

        /// <summary>
        /// Sends a message (multipart: the client id, the text and up to ten files).
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="form">The client id and the text.</param>
        /// <param name="files">The attached files.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The message as the members see it.</returns>
        private static async Task<Results<Ok<ChatMessageDto>, ProblemHttpResult>> HandleAsync(
            Guid id,
            [FromForm] SendChatMessageForm form,
            IFormFileCollection? files,
            IRequestHandler<Command, Result<ChatMessageDto>> handler,
            CancellationToken cancellationToken)
            => (await FormFiles.WithUploadsAsync(files, uploads => handler.HandleAsync(new Command(id, new SendChatMessage(form.ClientId, form.Body, uploads)), cancellationToken))).ToOk();
    }
}
