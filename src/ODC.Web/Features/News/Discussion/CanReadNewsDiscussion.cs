using ODC.Web.Messaging;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Whether the current user may read an article's discussion: asked by the SignalR hub before a page joins the
/// article's group of live updates.
/// </summary>
public static class CanReadNewsDiscussion
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="ArticleId">The article.</param>
    public sealed record Query(Guid ArticleId) : IQuery<bool>;

    /// <summary>
    /// Checks the rights.
    /// </summary>
    /// <param name="access">The discussion rights.</param>
    internal sealed class Handler(DiscussionAccess access) : IRequestHandler<Query, bool>
    {
        /// <inheritdoc />
        public async Task<bool> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await access.OfAsync(request.ArticleId, cancellationToken) is not null;
        }
    }
}
