namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// Tells the open pages of an article that its reactions or comments changed, so they load the discussion again. The web
/// layer pushes the signal over SignalR (with several servers, a SignalR backplane carries it to all of them).
/// </summary>
public interface INewsDiscussionEvents
{
    /// <summary>
    /// Notifies the pages of an article; never waits for them and never fails the change that caused it.
    /// </summary>
    /// <param name="articleId">The article.</param>
    void Publish(Guid articleId);
}
