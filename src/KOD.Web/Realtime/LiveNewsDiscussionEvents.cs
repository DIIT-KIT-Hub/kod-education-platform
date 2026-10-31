using KOD.Application.Features.News.Discussion;
using Microsoft.AspNetCore.SignalR;

namespace KOD.Web.Realtime;

/// <summary>
/// Pushes discussion changes to the pages in the article's group of <see cref="LiveHub"/>.
/// </summary>
/// <param name="hub">The hub context.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class LiveNewsDiscussionEvents(IHubContext<LiveHub, ILiveClient> hub, ILogger<LiveNewsDiscussionEvents> logger) : INewsDiscussionEvents
{
    /// <inheritdoc />
    public void Publish(Guid articleId)
    {
        // A slow or gone page must not delay the request that made the change.
        _ = SendAsync(articleId);
    }

    /// <summary>
    /// Logs a signal that could not be sent.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="articleId">The article.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The discussion change of article {ArticleId} was not sent to the open pages")]
    private static partial void LogSendFailed(ILogger logger, Guid articleId, Exception exception);

    /// <summary>
    /// Sends the signal and logs a failure instead of throwing.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns>A task that completes when sent.</returns>
    private async Task SendAsync(Guid articleId)
    {
        try
        {
            await hub.Clients.Group(LiveHub.NewsGroup(articleId)).NewsDiscussionChanged(articleId);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogSendFailed(logger, articleId, exception);
        }
    }
}
