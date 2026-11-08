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
/// Deletes an own message for everyone; a placeholder keeps its number.
/// </summary>
public static class DeleteChatMessage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="MessageId">The message.</param>
    public sealed record Command(Guid ChatId, Guid MessageId) : ICommand<Result>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, ChatRoom room) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var messageId = request.MessageId;
            var found = await room.OwnMessageAsync(chatId, messageId, Texts.T("Видаляти можна лише власні повідомлення."), cancellationToken);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            IReadOnlyList<ChatAttachment> removed;
            try
            {
                removed = found.Value.Delete();
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
            }

            db.ChatAttachments.RemoveRange(removed);
            db.ChatReactions.RemoveRange(await db.ChatReactions.Where(r => r.MessageId == messageId).ToListAsync(cancellationToken));
            await db.SaveChangesAsync(cancellationToken);
            await room.DeleteFilesAsync(removed.SelectMany(static a => new[] { a.StorageKey, a.ThumbnailKey }).OfType<string>(), cancellationToken);

            var published = await room.PublishChangedAsync(chatId, messageId, cancellationToken);
            return published.IsSuccess ? Result.Success() : published.Error;
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
            ChatRoom.MapChats(api).MapDelete("{id:guid}/messages/{messageId:guid}", HandleAsync);
        }

        /// <summary>
        /// Deletes an own message for everyone.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="messageId">The message.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, Guid messageId, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, messageId), cancellationToken)).ToNoContent();
    }
}
