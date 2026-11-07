using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ODC.Application.Common.Models;
using ODC.Application.Features.Chats;
using ODC.Application.Features.News.Discussion;
using ODC.Domain.Access;
using ODC.Web.Security;

namespace ODC.Web.Realtime;

/// <summary>
/// The one real-time connection of the client application (SignalR). Chat messages go to the connections of the
/// members (users, not groups: the members are read from the database for every message); a news page joins the group
/// of its article's discussion. Every change itself goes through the REST API (antiforgery, validation); the hub only
/// lets a signed-in user listen to what they may read and tell the members of a chat that they are typing. A connection
/// closes when its access token expires and the client reconnects with a fresh one, so a revoked sign-in stops
/// receiving within minutes.
/// </summary>
/// <param name="discussion">The news discussion service (access checks).</param>
/// <param name="chats">The chat service (membership).</param>
[Authorize]
public sealed class LiveHub(INewsDiscussionService discussion, IChatService chats) : Hub<ILiveClient>
{
    /// <summary>
    /// The address of the hub (under the API: 401 without a sign-in, 403 until the account setup is finished).
    /// </summary>
    public const string Route = "/api/v1/live";

    /// <summary>
    /// The connection item with the moment of the last typing signal.
    /// </summary>
    private const string TypingItem = "odc:typing";

    /// <summary>
    /// The shortest interval between two typing signals of one page, in milliseconds.
    /// </summary>
    private const long TypingInterval = 2000;

    /// <summary>
    /// Gets the group of the pages that show an article's discussion.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns>The group name.</returns>
    public static string NewsGroup(Guid articleId) => $"news:{articleId:N}";

    /// <summary>
    /// Accepts connections from the application's own pages only (the cookies of a signed-in user must not let another
    /// site listen).
    /// </summary>
    /// <returns>A task that completes when the connection is set up.</returns>
    public override Task OnConnectedAsync()
    {
        var request = Context.GetHttpContext()?.Request;
        var origin = request?.Headers.Origin.ToString();
        if (request is null || (!string.IsNullOrEmpty(origin) && !string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase)))
        {
            Context.Abort();
        }

        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Starts sending the changes of an article's discussion to this page.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns><see langword="true"/> when joined; <see langword="false"/> when the article is not readable.</returns>
    public async Task<bool> JoinNewsDiscussion(Guid articleId)
    {
        if (Context.User?.HasPermission(Permissions.News.View) != true || !await discussion.CanReadAsync(articleId, Context.ConnectionAborted))
        {
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, NewsGroup(articleId), Context.ConnectionAborted);
        return true;
    }

    /// <summary>
    /// Stops sending the changes of an article's discussion to this page.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns>A task that completes when left.</returns>
    public Task LeaveNewsDiscussion(Guid articleId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, NewsGroup(articleId), Context.ConnectionAborted);

    /// <summary>
    /// Tells the other members of a chat that the user is typing (at most once in two seconds per page).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <returns>A task that completes when the signal is sent.</returns>
    public async Task Typing(Guid chatId)
    {
        var user = Context.User;
        if (user?.HasPermission(Permissions.Chats.Use) != true || user.UserId is not { } me)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (Context.Items.TryGetValue(TypingItem, out var last) && last is long previous && now - previous < TypingInterval)
        {
            return;
        }

        Context.Items[TypingItem] = now;
        var members = await chats.GetMemberIdsAsync(chatId, Context.ConnectionAborted);
        if (members is null)
        {
            return;
        }

        var others = members.Where(id => id != me).Select(static id => id.ToString()).ToList();
        await Clients.Users(others).ChatTyping(chatId, new UserRef(me, user.FullName, user.PhotoId));
    }
}
