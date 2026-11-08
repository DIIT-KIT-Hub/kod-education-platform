using ODC.Web.Messaging;

namespace ODC.Web.Features.Chats;

/// <summary>
/// The members of a chat when the current user is one of them (the SignalR hub relays typing signals to them).
/// </summary>
public static class GetChatMemberIds
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    public sealed record Query(Guid ChatId) : IQuery<IReadOnlyList<Guid>?>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(ChatRoom room) : IRequestHandler<Query, IReadOnlyList<Guid>?>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<Guid>?> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            if (room.Me is not { } me)
            {
                return null;
            }

            var members = await room.MemberIdsAsync(chatId, cancellationToken);
            return members.Contains(me) ? members : null;
        }
    }
}
