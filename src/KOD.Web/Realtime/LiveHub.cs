using KOD.Application.Features.News.Discussion;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KOD.Web.Realtime;

/// <summary>
/// The one real-time connection of the client application (SignalR). A page joins the groups it shows — the discussion
/// of a news article now, chats later — and receives signals when they change. Every change itself goes through the
/// REST API (antiforgery, validation, audit); the hub only lets a signed-in user listen to what they may read.
/// </summary>
/// <param name="discussion">The news discussion service (access checks).</param>
[Authorize]
public sealed class LiveHub(INewsDiscussionService discussion) : Hub<ILiveClient>
{
    /// <summary>
    /// The address of the hub (under the API: 401 without a sign-in, 403 until the account setup is finished).
    /// </summary>
    public const string Route = "/api/v1/live";

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
}
