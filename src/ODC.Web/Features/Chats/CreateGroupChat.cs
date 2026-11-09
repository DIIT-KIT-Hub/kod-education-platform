using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Creates a group chat; the creator becomes its owner.
/// </summary>
public static class CreateGroupChat
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Title">The title.</param>
    /// <param name="MemberIds">The member ids.</param>
    public sealed record Command(string? Title, IReadOnlyCollection<Guid> MemberIds) : ICommand<Result<Guid>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="events">Delivers changes to the members' pages.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, IChatEvents events, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var title = request.Title;
            var memberIds = request.MemberIds;
            ArgumentNullException.ThrowIfNull(memberIds);
            if (room.Me is not { } me)
            {
                return Error.Forbidden();
            }

            var invalid = await room.CheckPeopleAsync(memberIds, cancellationToken);
            if (invalid is not null)
            {
                return invalid;
            }

            try
            {
                var moment = timeProvider.GetUtcNow();
                var chat = Chat.Group(me, title, memberIds, moment);
                db.Chats.Add(chat);
                var created = new ChatMessage(chat.Id, chat.NextSequence(moment), me, ChatEvent.Created, chat.Title);
                db.ChatMessages.Add(created);
                chat.MemberOf(me)!.MarkRead(created.Sequence);
                await db.SaveChangesAsync(cancellationToken);
                events.ChatChanged([.. chat.Members.Select(static m => m.UserId)], chat.Id);
                return chat.Id;
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
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
            ChatRoom.MapChats(api).MapPost(string.Empty, HandleAsync);
        }

        /// <summary>
        /// Creates a group chat.
        /// </summary>
        /// <param name="group">The title and the members.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the chat.</returns>
        private static async Task<Results<Created<ChatIdResponse>, ProblemHttpResult>> HandleAsync(CreateGroupChatRequest group, IRequestHandler<Command, Result<Guid>> handler, CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(group.Title, group.MemberIds), cancellationToken);
            return result.IsSuccess ? TypedResults.Created($"{ApiEndpoints.Base}/chats/{result.Value}", new ChatIdResponse(result.Value)) : result.Error.ToProblem();
        }
    }
}
