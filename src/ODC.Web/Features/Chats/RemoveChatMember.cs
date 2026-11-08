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
/// Removes a member (the owner) or leaves a group chat; the last member leaving deletes the chat with its files.
/// </summary>
public static class RemoveChatMember
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="UserId">The user.</param>
    public sealed record Command(Guid ChatId, Guid UserId) : ICommand<Result>;

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
            var userId = request.UserId;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var members = await room.MemberIdsAsync(chatId, cancellationToken);
            if (!members.Contains(me))
            {
                return ChatRoom.NotFound();
            }

            if (members.Count == 1 && userId == me)
            {
                // The last member leaves: nothing of the chat is kept.
                var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
                if (chat.Kind != ChatKind.Group)
                {
                    return Error.Validation(Texts.T("Це можна зробити лише в груповому чаті."));
                }

                await room.RemoveChatAsync(chat, [userId], cancellationToken);
                return Result.Success();
            }

            var details = userId == me ? null : await room.NamesAsync([userId], cancellationToken);
            Task<ChatRoom.ChatChange?> Remove(Chat chat, Guid actor)
            {
                chat.RemoveMember(actor, userId);
                return Task.FromResult<ChatRoom.ChatChange?>(new ChatRoom.ChatChange(userId == me ? ChatEvent.MemberLeft : ChatEvent.MemberRemoved, details, [userId]));
            }

            var result = await room.ChangeAsync(chatId, Remove, cancellationToken);
            return result.IsSuccess ? Result.Success() : result.Error;
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
            ChatRoom.MapChats(api).MapDelete("{id:guid}/members/{userId:guid}", HandleAsync);
        }

        /// <summary>
        /// Removes a member or leaves a group chat.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="userId">The member.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, Guid userId, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, userId), cancellationToken)).ToNoContent();
    }
}
