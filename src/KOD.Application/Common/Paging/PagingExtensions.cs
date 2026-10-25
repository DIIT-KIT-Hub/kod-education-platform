using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Paging;

/// <summary>
/// EF Core helpers that turn an <see cref="IQueryable{T}"/> into a <see cref="PagedResult{T}"/>.
/// </summary>
public static class PagingExtensions
{
    /// <param name="query">The filtered and ordered query.</param>
    /// <typeparam name="T">The item type.</typeparam>
    extension<T>(IQueryable<T> query)
    {
        /// <summary>
        /// Executes the query for a single page. When the requested page is beyond the end, the last page is returned.
        /// </summary>
        /// <param name="request">The paging parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        public async Task<PagedResult<T>> ToPagedResultAsync(PageRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var total = await query.CountAsync(cancellationToken);
            if (total == 0)
            {
                return PagedResult<T>.Empty(request);
            }

            var lastPage = (int)Math.Ceiling(total / (double)request.PageSize);
            var page = Math.Min(request.Page, lastPage);
            var items = await query
                .Skip((page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, page, request.PageSize, total);
        }

        /// <summary>
        /// Applies an ascending or descending order by the given key.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="descending">Whether to sort descending.</param>
        /// <returns>The ordered query.</returns>
        public IOrderedQueryable<T> OrderByDirection<TKey>(System.Linq.Expressions.Expression<Func<T, TKey>> keySelector, bool descending)
            => descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
