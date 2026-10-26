using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.News;

/// <summary>
/// News feed and news management use cases.
/// </summary>
public interface INewsService
{
    /// <summary>
    /// Lists news. Drafts are included only for news managers and when requested.
    /// </summary>
    /// <param name="query">The paging and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of news.</returns>
    Task<Result<PagedResult<NewsListItem>>> ListAsync(NewsQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a published article by its slug (managers can also open drafts).
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The article.</returns>
    Task<Result<NewsArticleDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an article for editing.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The article.</returns>
    Task<Result<NewsArticleDto>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an article.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new article identifier.</returns>
    Task<Result<Guid>> CreateAsync(SaveNewsCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an article.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(Guid id, SaveNewsCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an article.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the current user as a reader of an article (once per reader).
    /// </summary>
    /// <param name="id">The article identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when recorded.</returns>
    Task RecordViewAsync(Guid id, CancellationToken cancellationToken);
}
