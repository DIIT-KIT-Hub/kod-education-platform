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
/// Deletes a chat for everyone (either person of a one-to-one chat, the owner of a group).
/// </summary>
public static class DeleteChat
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
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ApplicationDbContext db, ChatRoom room) : IRequestHandler<Command, Result>
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

            var chat = await db.Chats.Include(static c => c.Members).FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
            if (chat?.MemberOf(me) is not { } member)
            {
                return ChatRoom.NotFound();
            }

            if (chat.Kind == ChatKind.Group && member.Role != ChatMemberRole.Owner)
            {
                return Error.Forbidden(Texts.T("Видалити груповий чат для всіх може лише його власник."));
            }

            await room.RemoveChatAsync(chat, [.. chat.Members.Select(static m => m.UserId)], cancellationToken);
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
            ChatRoom.MapChats(api).MapDelete("{id:guid}", HandleAsync);
        }

        /// <summary>
        /// Deletes a chat for everyone.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
