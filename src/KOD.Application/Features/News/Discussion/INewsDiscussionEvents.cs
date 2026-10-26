namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// Tells open article pages that the reactions or comments of the article changed, so they refresh at once.
/// The default implementation works within one server process; with several servers, replace it with one backed by a
/// message bus (for example PostgreSQL <c>LISTEN/NOTIFY</c>) — the subscribers do not change.
/// </summary>
public interface INewsDiscussionEvents
{
    /// <summary>
    /// Subscribes to the changes of an article.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="handler">Called after every change (on a background thread).</param>
    /// <returns>Ends the subscription when disposed.</returns>
    IDisposable Subscribe(Guid articleId, Func<Task> handler);

    /// <summary>
    /// Notifies the subscribers of an article.
    /// </summary>
    /// <param name="articleId">The article.</param>
    void Publish(Guid articleId);
}
