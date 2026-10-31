namespace KOD.Web.Realtime;

/// <summary>
/// The messages <see cref="LiveHub"/> sends to the pages. They are signals only: the page loads the data itself through
/// the REST API, with the usual access checks.
/// </summary>
public interface ILiveClient
{
    /// <summary>
    /// The reactions or comments of an article changed.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    Task NewsDiscussionChanged(Guid articleId);
}
