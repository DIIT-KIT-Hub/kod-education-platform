using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Finds people to write to: every word must match a part of the name or the e-mail.
/// </summary>
public static class SearchChatPeople
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Term">The term.</param>
    public sealed record Query(string? Term) : IQuery<IReadOnlyList<UserLookupItem>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="room">The shared chat rules.</param>
    /// <param name="directory">The positions of people.</param>
    internal sealed class Handler(ChatRoom room, PositionDirectory directory) : IRequestHandler<Query, IReadOnlyList<UserLookupItem>>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<UserLookupItem>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var term = request.Term;
            var words = (term ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (room.Me is not { } me || words.Length == 0 || string.Join(' ', words).Length < 2)
            {
                return [];
            }

            // Every word must match a part of the name or the e-mail ("Іван Петренко" finds "Петренко Іван").
            var people = (await room.ChatUsersAsync(cancellationToken)).Where(u => u.Id != me);
            foreach (var word in words.Take(4))
            {
                people = people.Where(u => u.LastName.Contains(word) || u.FirstName.Contains(word) || (u.MiddleName != null && u.MiddleName.Contains(word)) || u.Email.Contains(word));
            }

            return await directory.LookupAsync(people.OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName).Take(20), cancellationToken);
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
            ChatRoom.MapChats(api).MapGet("people", HandleAsync);
        }

        /// <summary>
        /// Finds people to write to.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="term">The search text.</param>
        /// <returns>The people.</returns>
        private static async Task<Ok<IReadOnlyList<UserLookupItem>>> HandleAsync(IRequestHandler<Query, IReadOnlyList<UserLookupItem>> handler, CancellationToken cancellationToken, string? term = null)
            => TypedResults.Ok(await handler.HandleAsync(new Query(term), cancellationToken));
    }
}
