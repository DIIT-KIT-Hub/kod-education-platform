using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Renames a group chat (any member).
/// </summary>
public static class RenameChat
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    /// <param name="Title">The title.</param>
    public sealed record Command(Guid ChatId, string? Title) : ICommand<Result>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ChatRoom room) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            var title = request.Title;
            Task<ChatRoom.ChatChange?> Rename(Chat chat, Guid actor)
            {
                chat.Rename(title);
                return Task.FromResult<ChatRoom.ChatChange?>(new ChatRoom.ChatChange(ChatEvent.Renamed, chat.Title, []));
            }

            var result = await room.ChangeAsync(chatId, Rename, cancellationToken);
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
            ChatRoom.MapChats(api).MapPut("{id:guid}/title", HandleAsync);
        }

        /// <summary>
        /// Renames a group chat.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="title">The new title.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, ChatTitleRequest title, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, title.Title), cancellationToken)).ToNoContent();
    }
}
