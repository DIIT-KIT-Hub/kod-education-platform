using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.News;

/// <summary>
/// The news feed: everyone reads the published news of every organization; the management view (drafts included) lists
/// only the articles the user manages.
/// </summary>
public static class ListNews
{
    /// <summary>
    /// Paging and filter parameters of the news list.
    /// </summary>
    [RequirePermission(Permissions.News.View)]
    public sealed record Query : PageRequest, IQuery<Result<PagedResult<NewsListItem>>>
    {
        /// <summary>
        /// Gets the category filter.
        /// </summary>
        public NewsCategory? Category { get; init; }

        /// <summary>
        /// Gets a value indicating whether drafts are included (news managers: the articles they manage).
        /// </summary>
        public bool? IncludeDrafts { get; init; }
    }

    /// <summary>
    /// Filters, sorts and pages the articles with their counters.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared news rules.</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, NewsDesk desk) : IRequestHandler<Query, Result<PagedResult<NewsListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<NewsListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var articles = db.NewsArticles.AsNoTracking();
            if (request.IncludeDrafts == true && currentUser.Has(Permissions.News.Manage))
            {
                articles = NewsDesk.Manageable(articles, await desk.ManageScopeAsync(cancellationToken));
            }
            else
            {
                articles = articles.Where(static a => a.PublishedAt != null);
            }

            if (request.Category is { } category)
            {
                articles = articles.Where(a => a.Category == category);
            }

            if (request.Search is { } search)
            {
                var term = search;
                articles = articles.Where(a => a.Title.Contains(term) || a.Summary.Contains(term));
            }

            articles = request.SortBy switch
            {
                "title" => articles.OrderByDirection(static a => a.Title, request.IsDescending),
                "updated" => articles.OrderByDirection(static a => a.UpdatedAt ?? a.CreatedAt, request.IsDescending),
                _ => articles.OrderByDescending(static a => a.IsPinned).ThenByDescending(static a => a.PublishedAt ?? a.CreatedAt),
            };

            return await articles
                .Select(a => new NewsListItem(
                    a.Id,
                    a.Slug,
                    a.Title,
                    a.Summary,
                    a.Category,
                    a.CoverImageUrl,
                    a.IsPinned,
                    a.PublishedAt,
                    a.Author!.LastName + " " + a.Author.FirstName,
                    a.Author.PhotoId,
                    a.UpdatedAt ?? a.CreatedAt,
                    a.Organization != null ? new UnitRef(a.Organization.Id, a.Organization.Name) : null,
                    db.NewsComments.Count(c => c.ArticleId == a.Id && !c.IsDeleted),
                    db.NewsReactions.Count(r => r.ArticleId == a.Id && r.CommentId == null),
                    db.NewsViews.Count(v => v.ArticleId == a.Id)))
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/news</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => NewsDesk.MapNews(api).MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists published news (managers may include drafts).
        /// </summary>
        /// <param name="query">The filters and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<NewsListItem>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] Query query,
            IRequestHandler<Query, Result<PagedResult<NewsListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }
}
