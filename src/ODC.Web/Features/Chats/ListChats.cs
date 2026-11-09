using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;

namespace ODC.Web.Features.Chats;

/// <summary>
/// The chats of the current user, the latest activity first, with the unread count and the last message.
/// </summary>
public static class ListChats
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Query : IQuery<IReadOnlyList<ChatSummary>>;

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="room">The shared chat rules.</param>
    internal sealed class Handler(OrganizationDbContext db, ChatRoom room) : IRequestHandler<Query, IReadOnlyList<ChatSummary>>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<ChatSummary>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (room.Me is not { } me)
            {
                return [];
            }

            // A one-to-one chat appears for the other person with its first message, and leaves the list of a person who
            // deleted it for themselves until a newer message arrives.
            var rows = await (
                    from m in db.ChatMembers
                    where m.UserId == me
                    join c in db.Chats on m.ChatId equals c.Id
                    where c.Kind == ChatKind.Group || c.LastSequence > 0 || c.CreatedById == me
                    where c.Kind == ChatKind.Group || m.ClearedSequence == 0 || c.LastSequence > m.ClearedSequence
                    orderby c.LastActivityAt descending
                    select new
                    {
                        c.Id,
                        c.Kind,
                        c.Title,
                        c.LastSequence,
                        c.LastActivityAt,
                        m.LastReadSequence,
                        MemberCount = db.ChatMembers.Count(x => x.ChatId == c.Id),
                        Unread = db.ChatMessages.Count(x => x.ChatId == c.Id
                            && x.Sequence > m.LastReadSequence
                            && x.AuthorId != me
                            && x.Kind == ChatMessageKind.Text
                            && !x.IsDeleted
                            && !db.HiddenChatMessages.Any(h => h.MessageId == x.Id && h.UserId == me)),
                        Members = db.ChatMembers
                            .Where(x => x.ChatId == c.Id && x.UserId != me)
                            .OrderBy(static x => x.JoinedAt)
                            .Take(4)
                            .Select(static x => new ChatRoom.PersonRow(x.UserId, x.User!.LastName, x.User.FirstName, x.User.MiddleName, x.User.PhotoId))
                            .ToList(),
                        Last = db.ChatMessages
                            .Where(x => x.ChatId == c.Id
                                && x.Sequence > m.ClearedSequence
                                && !db.HiddenChatMessages.Any(h => h.MessageId == x.Id && h.UserId == me))
                            .OrderByDescending(static x => x.Sequence)
                            .Select(static x => new
                            {
                                Author = new ChatRoom.PersonRow(x.AuthorId, x.Author!.LastName, x.Author.FirstName, x.Author.MiddleName, x.Author.PhotoId),
                                x.Kind,
                                x.Event,
                                Text = x.Text.Length > ChatRoom.PreviewLength ? x.Text.Substring(0, ChatRoom.PreviewLength) : x.Text,
                                x.IsDeleted,
                                Attachments = x.Attachments.Count,
                                x.CreatedAt,
                            })
                            .FirstOrDefault(),
                    })
                .AsNoTracking()
                .Take(ChatRoom.MaxChats)
                .ToListAsync(cancellationToken);

            return rows.ConvertAll(static r => new ChatSummary(
                r.Id,
                r.Kind,
                r.Title,
                r.Members.ConvertAll(static p => p.ToRef()),
                r.MemberCount,
                r.Last is { } last ? new ChatPreview(last.Author.ToRef(), last.Kind, last.Event, last.Text, last.IsDeleted, last.Attachments, last.CreatedAt) : null,
                r.LastActivityAt,
                r.LastSequence,
                r.LastReadSequence,
                r.Unread));
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
            ChatRoom.MapChats(api).MapGet(string.Empty, HandleAsync);
        }

        /// <summary>
        /// Lists the chats of the current user.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chats.</returns>
        private static async Task<Ok<IReadOnlyList<ChatSummary>>> HandleAsync(IRequestHandler<Query, IReadOnlyList<ChatSummary>> handler, CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(new Query(), cancellationToken));
    }
}
