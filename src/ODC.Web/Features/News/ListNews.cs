using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
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
    /// <param name="general">Reads the news of the whole cabinet held by other organizations.</param>
    /// <param name="organization">The organization of the request (a guest sees only its news of the whole cabinet).</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, NewsDesk desk, GeneralNews general, OrganizationContext organization) : IRequestHandler<Query, Result<PagedResult<NewsListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<NewsListItem>>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var drafts = request.IncludeDrafts == true && currentUser.Has(Permissions.News.Manage);
            var articles = Filter(db.NewsArticles.AsNoTracking(), request, drafts);
            if (drafts)
            {
                articles = NewsDesk.Manageable(articles, await desk.ManageScopeAsync(cancellationToken));
            }
            else if (organization.Access == OrganizationAccess.Guest)
            {
                articles = articles.Where(static a => a.OrganizationId == null);
            }

            if (drafts)
            {
                return await Project(Order(articles, request), db).ToPagedResultAsync(request, cancellationToken);
            }

            // The news of the whole cabinet written in other organizations join the page (each gives at most the page's end).
            var upTo = request.CurrentPage * request.CurrentPageSize;
            var total = await articles.CountAsync(cancellationToken);
            var candidates = await Project(Order(articles, request), db).Take(upTo).ToListAsync(cancellationToken);
            foreach (var other in await general.FromOthersAsync(
                async (otherDb, ids) =>
                {
                    var theirs = Filter(otherDb.NewsArticles.AsNoTracking().Where(a => ids.Contains(a.Id) && a.OrganizationId == null), request, drafts: false);
                    return (Count: await theirs.CountAsync(cancellationToken), Items: await Project(Order(theirs, request), otherDb).Take(upTo).ToListAsync(cancellationToken));
                },
                cancellationToken))
            {
                total += other.Value.Count;
                candidates.AddRange(other.Value.Items.Select(i => i with { OrganizationCode = other.Organization.Code }));
            }

            var page = Order(candidates.AsQueryable(), request).Skip((request.CurrentPage - 1) * request.CurrentPageSize).Take(request.CurrentPageSize).ToList();
            return new PagedResult<NewsListItem>(page, request.CurrentPage, request.CurrentPageSize, total);
        }

        /// <summary>
        /// Applies the filters of the list (published only unless drafts are asked for, category, search).
        /// </summary>
        /// <param name="articles">The articles.</param>
        /// <param name="request">The filters.</param>
        /// <param name="drafts">Whether drafts are listed too.</param>
        /// <returns>The filtered articles.</returns>
        private static IQueryable<NewsArticle> Filter(IQueryable<NewsArticle> articles, Query request, bool drafts)
        {
            if (!drafts)
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

            return articles;
        }

        /// <summary>
        /// Orders articles as the list asks (pinned and newest first by default).
        /// </summary>
        /// <param name="articles">The articles.</param>
        /// <param name="request">The sorting.</param>
        /// <returns>The ordered articles.</returns>
        private static IQueryable<NewsArticle> Order(IQueryable<NewsArticle> articles, Query request) => request.SortBy switch
        {
            "title" => articles.OrderByDirection(static a => a.Title, request.IsDescending),
            "updated" => articles.OrderByDirection(static a => a.UpdatedAt ?? a.CreatedAt, request.IsDescending),
            _ => articles.OrderByDescending(static a => a.IsPinned).ThenByDescending(static a => a.PublishedAt ?? a.CreatedAt),
        };

        /// <summary>
        /// Orders list items as the list asks (the merge of several organizations).
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="request">The sorting.</param>
        /// <returns>The ordered items.</returns>
        private static IQueryable<NewsListItem> Order(IQueryable<NewsListItem> items, Query request) => request.SortBy switch
        {
            "title" => items.OrderByDirection(static a => a.Title, request.IsDescending),
            "updated" => items.OrderByDirection(static a => a.UpdatedAt, request.IsDescending),
            _ => items.OrderByDescending(static a => a.IsPinned).ThenByDescending(static a => a.PublishedAt ?? a.UpdatedAt),
        };

        /// <summary>
        /// Projects articles into list items.
        /// </summary>
        /// <param name="articles">The ordered articles.</param>
        /// <param name="db">Their organization's database (comments, reactions, views).</param>
        /// <returns>The items.</returns>
        private static IQueryable<NewsListItem> Project(IQueryable<NewsArticle> articles, OrganizationDbContext db)
            => articles
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
                    db.NewsViews.Count(v => v.ArticleId == a.Id)));
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
