using ODC.Application.Features.News.Discussion;

namespace ODC.UnitTests.Application;

/// <summary>
/// Discussion events that go nowhere (the tests read the database instead of open pages).
/// </summary>
internal sealed class SilentDiscussionEvents : INewsDiscussionEvents
{
    /// <inheritdoc />
    public void Publish(Guid articleId)
    {
    }
}
