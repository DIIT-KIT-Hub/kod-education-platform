using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Gets a chat with its members (its members only).
/// </summary>
public static class GetChat
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ChatId">The chat.</param>
    public sealed record Query(Guid ChatId) : IQuery<Result<ChatDetails>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="room">The shared chat rules.</param>
    /// <param name="directory">The positions of people.</param>
    internal sealed class Handler(OrganizationDbContext db, ChatRoom room, PositionDirectory directory) : IRequestHandler<Query, Result<ChatDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<ChatDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var chatId = request.ChatId;
            if (room.Me is not { } me)
            {
                return ChatRoom.NotFound();
            }

            var chat = await db.Chats
                .AsNoTracking()
                .Where(c => c.Id == chatId && c.Members.Any(m => m.UserId == me))
                .Select(static c => new { c.Id, c.Kind, c.Title, c.LastSequence })
                .FirstOrDefaultAsync(cancellationToken);
            if (chat is null)
            {
                return ChatRoom.NotFound();
            }

            var members = await db.ChatMembers
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .Select(static m => new
                {
                    Person = new ChatRoom.PersonRow(m.UserId, m.User!.LastName, m.User.FirstName, m.User.MiddleName, m.User.PhotoId),
                    m.Role,
                    m.LastReadSequence,
                    m.User.IsActive,
                })
                .ToListAsync(cancellationToken);

            var positions = await directory.PrimaryAsync(members.Select(static m => m.Person.Id), cancellationToken);
            var list = members
                .OrderByDescending(static m => m.Role)
                .ThenBy(static m => m.Person.LastName, StringComparer.CurrentCulture)
                .ThenBy(static m => m.Person.FirstName, StringComparer.CurrentCulture)
                .Select(m => new ChatMemberDto(m.Person.ToRef(), m.Role, positions.GetValueOrDefault(m.Person.Id)?.InUnit, m.LastReadSequence, m.IsActive))
                .ToList();
            var myRole = members.First(m => m.Person.Id == me).Role;
            return new ChatDetails(chat.Id, chat.Kind, chat.Title, list, chat.LastSequence, myRole);
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
            ChatRoom.MapChats(api).MapGet("{id:guid}", HandleAsync);
        }

        /// <summary>
        /// Gets a chat with its members.
        /// </summary>
        /// <param name="id">The chat.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat or the problem.</returns>
        private static async Task<Results<Ok<ChatDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<ChatDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();
    }
}
