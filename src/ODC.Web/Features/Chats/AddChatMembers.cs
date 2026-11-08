using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Adds people to a group chat (any member).
/// </summary>
public static class AddChatMembers
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="UserIds">The user ids.</param>
    public sealed record Command(Guid ChatId, IReadOnlyCollection<Guid> UserIds) : ICommand<Result>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var userIds = request.UserIds;
            ArgumentNullException.ThrowIfNull(userIds);
            var invalid = await room.CheckPeopleAsync(userIds, cancellationToken);
            if (invalid is not null)
            {
                return invalid;
            }

            async Task<ChatRoom.ChatChange?> Add(Chat chat, Guid actor)
            {
                var added = chat.AddMembers(userIds, timeProvider.GetUtcNow());
                if (added.Count == 0)
                {
                    return null;
                }

                return new ChatRoom.ChatChange(ChatEvent.MembersAdded, await room.NamesAsync(added, cancellationToken), []);
            }

            var result = await room.ChangeAsync(chatId, Add, cancellationToken);
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
            ChatRoom.MapChats(api).MapPost("{id:guid}/members", HandleAsync);
        }

        /// <summary>
        /// Adds people to a group chat.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="members">The people.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, ChatMembersRequest members, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, members.UserIds), cancellationToken)).ToNoContent();
    }
}
