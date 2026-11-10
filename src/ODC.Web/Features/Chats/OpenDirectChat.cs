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
/// Opens the one-to-one chat with a person, creating it once (unique per pair).
/// </summary>
public static class OpenDirectChat
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="UserId">The user.</param>
    public sealed record Command(Guid UserId) : ICommand<Result<Guid>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, TimeProvider timeProvider, ChatRoom room) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var userId = request.UserId;
            if (room.Me is not { } me)
            {
                return Error.Forbidden();
            }

            if (userId == me)
            {
                return Error.Validation(Texts.T("Не можна почати чат із самим собою."));
            }

            var key = Chat.DirectKeyOf(me, userId);
            var existing = await db.Chats.Where(c => c.DirectKey == key).Select(static c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
            if (existing is { } id)
            {
                return id;
            }

            if (await room.CheckPeopleAsync([userId], cancellationToken) is { } invalid)
            {
                return invalid;
            }

            var chat = Chat.Direct(me, userId, timeProvider.GetUtcNow());
            db.Chats.Add(chat);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The other person opened the same chat at the same moment.
                db.DiscardChanges();
                return await db.Chats.Where(c => c.DirectKey == key).Select(static c => c.Id).FirstAsync(cancellationToken);
            }

            return chat.Id;
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
            ChatRoom.MapChats(api).MapPost("direct", HandleAsync);
        }

        /// <summary>
        /// Opens the one-to-one chat with a person.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat.</returns>
        private static async Task<Results<Ok<ChatIdResponse>, ProblemHttpResult>> HandleAsync(OpenDirectChatRequest person, IRequestHandler<Command, Result<Guid>> handler, CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(person.UserId), cancellationToken);
            return result.IsSuccess ? TypedResults.Ok(new ChatIdResponse(result.Value)) : result.Error.ToProblem();
        }
    }
}
