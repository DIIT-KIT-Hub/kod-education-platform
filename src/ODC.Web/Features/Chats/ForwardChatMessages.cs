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
/// Forwards messages into another chat as the user's new messages (their files are copied).
/// </summary>
public static class ForwardChatMessages
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="SourceChatId">The source chat.</param>
    /// <param name="MessageIds">The message ids.</param>
    public sealed record Command(Guid ChatId, Guid SourceChatId, IReadOnlyCollection<Guid> MessageIds) : ICommand<Result<IReadOnlyList<ChatMessageDto>>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="events">Delivers changes to the members' pages.</param>
    /// <param name="domainEvents">Raises the new messages (the notification of the members).</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, IChatEvents events, DomainEvents domainEvents, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<IReadOnlyList<ChatMessageDto>>>
    {
        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<ChatMessageDto>>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var sourceChatId = request.SourceChatId;
            var messageIds = request.MessageIds;
            ArgumentNullException.ThrowIfNull(messageIds);
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var ids = messageIds.Distinct().ToList();
            if (ids.Count == 0 || ids.Count > ChatRoom.MaxForwarded)
            {
                return Error.Validation(Texts.T("Оберіть від 1 до {0} повідомлень.", ChatRoom.MaxForwarded));
            }

            // Both chats must be the user's; only messages the user still sees can be forwarded.
            var cleared = await db.ChatMembers
                .Where(m => m.ChatId == sourceChatId && m.UserId == me)
                .Select(static m => (long?)m.ClearedSequence)
                .FirstOrDefaultAsync(cancellationToken);
            if (cleared is null || !(await room.MemberIdsAsync(chatId, cancellationToken)).Contains(me))
            {
                return ChatRoom.NotFound();
            }

            var originals = await db.ChatMessages
                .AsNoTracking()
                .Include(static m => m.Attachments)
                .Where(m => m.ChatId == sourceChatId
                    && ids.Contains(m.Id)
                    && m.Kind == ChatMessageKind.Text
                    && !m.IsDeleted
                    && m.Sequence > cleared.Value
                    && !db.HiddenChatMessages.Any(h => h.MessageId == m.Id && h.UserId == me))
                .OrderBy(static m => m.Sequence)
                .ToListAsync(cancellationToken);
            if (originals.Count != ids.Count)
            {
                return Error.Validation(Texts.T("Деякі з цих повідомлень не можна переслати."));
            }

            var stored = new List<string>();
            try
            {
                var copies = new List<List<ChatRoom.ChatAttachmentCopy>>();
                foreach (var original in originals)
                {
                    var copied = new List<ChatRoom.ChatAttachmentCopy>();
                    foreach (var attachment in original.Attachments.OrderBy(static a => a.Position))
                    {
                        var key = await room.CopyFileAsync(attachment.StorageKey, stored, cancellationToken);
                        var thumbnail = attachment.ThumbnailKey is { } small ? await room.CopyFileAsync(small, stored, cancellationToken) : null;
                        copied.Add(new ChatRoom.ChatAttachmentCopy(attachment, key, thumbnail));
                    }

                    copies.Add(copied);
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

                    var added = new List<ChatMessage>();
                    for (var i = 0; i < originals.Count; i++)
                    {
                        var original = originals[i];
                        var attachments = copies[i].ConvertAll(static c => c.ToAttachment());
                        var message = new ChatMessage(chatId, chat.NextSequence(timeProvider.GetUtcNow()), me, Guid.NewGuid(), original.Body, original.Text, attachments);
                        message.MarkForwarded(original.ForwardedFromId ?? original.AuthorId);
                        db.ChatMessages.Add(message);
                        added.Add(message);
                    }

                    member.MarkRead(added[^1].Sequence);
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < ChatRoom.SendAttempts)
                    {
                        // Another message took a number: number these again.
                        db.DiscardChanges();
                        continue;
                    }

                    var addedIds = added.ConvertAll(static m => m.Id);
                    var dtos = await room.LoadAsync(db.ChatMessages.Where(m => addedIds.Contains(m.Id)).OrderBy(static m => m.Sequence), null, cancellationToken);
                    var recipients = await room.MemberIdsAsync(chatId, cancellationToken);
                    foreach (var dto in dtos)
                    {
                        events.MessageAdded(recipients, dto);
                    }

                    domainEvents.Raise(new ChatMessagesPosted(chatId, me, added[^1].Id));
                    return dtos;
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
            ChatRoom.MapChats(api).MapPost("{id:guid}/forward", HandleAsync);
        }

        /// <summary>
        /// Forwards messages of another chat into this one.
        /// </summary>
        /// <param name="id">The target chat.</param>
        /// <param name="forward">The source chat and the messages.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The new messages.</returns>
        private static async Task<Results<Ok<IReadOnlyList<ChatMessageDto>>, ProblemHttpResult>> HandleAsync(
            Guid id,
            ForwardChatMessagesRequest forward,
            IRequestHandler<Command, Result<IReadOnlyList<ChatMessageDto>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, forward.SourceChatId, forward.MessageIds), cancellationToken)).ToOk();
    }
}
