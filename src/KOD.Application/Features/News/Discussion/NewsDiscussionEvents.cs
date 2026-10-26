using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// The in-process <see cref="INewsDiscussionEvents"/> (singleton).
/// </summary>
internal sealed class NewsDiscussionEvents : INewsDiscussionEvents
{
    /// <summary>
    /// The handlers of each article.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ImmutableList<Func<Task>>> _handlers = new();

    /// <inheritdoc />
    public IDisposable Subscribe(Guid articleId, Func<Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.AddOrUpdate(articleId, _ => [handler], (_, list) => list.Add(handler));
        return new Subscription(this, articleId, handler);
    }

    /// <inheritdoc />
    public void Publish(Guid articleId)
    {
        if (!_handlers.TryGetValue(articleId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            // A slow or failed page must not delay the request that made the change.
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler();
                }
                catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException or TaskCanceledException)
                {
                    // The page was closed meanwhile.
                }
            });
        }
    }

    /// <summary>
    /// Removes a handler.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="handler">The handler.</param>
    private void Unsubscribe(Guid articleId, Func<Task> handler)
    {
        while (_handlers.TryGetValue(articleId, out var list))
        {
            var updated = list.Remove(handler);
            if (updated.IsEmpty
                ? _handlers.TryRemove(new KeyValuePair<Guid, ImmutableList<Func<Task>>>(articleId, list))
                : _handlers.TryUpdate(articleId, updated, list))
            {
                return;
            }
        }
    }

    /// <summary>
    /// A subscription that removes its handler when disposed.
    /// </summary>
    /// <param name="owner">The event hub.</param>
    /// <param name="articleId">The article.</param>
    /// <param name="handler">The handler.</param>
    private sealed class Subscription(NewsDiscussionEvents owner, Guid articleId, Func<Task> handler) : IDisposable
    {
        /// <summary>
        /// Whether the subscription has ended.
        /// </summary>
        private int _disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Unsubscribe(articleId, handler);
            }
        }
    }
}
