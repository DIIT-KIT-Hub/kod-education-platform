using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News;

/// <summary>
/// Gets an article: by its slug for readers (drafts only for their managers) and by its identifier for the editor.
/// </summary>
public static class GetNewsArticle
{
    /// <summary>
    /// An article for reading.
    /// </summary>
    /// <param name="Slug">The address of the article.</param>
    [RequirePermission(Permissions.News.View)]
    public sealed record BySlugQuery(string Slug) : IQuery<Result<NewsArticleDto>>;

    /// <summary>
    /// An article for editing (drafts included).
    /// </summary>
    /// <param name="Id">The article.</param>
    [RequirePermission(Permissions.News.Manage)]
    public sealed record ForEditingQuery(Guid Id) : IQuery<Result<NewsArticleDto>>;

    /// <summary>
    /// Reads an article for reading.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared news rules.</param>
    internal sealed class BySlugHandler(ApplicationDbContext db, NewsDesk desk) : IRequestHandler<BySlugQuery, Result<NewsArticleDto>>
    {
        /// <inheritdoc />
        public async Task<Result<NewsArticleDto>> HandleAsync(BySlugQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).Include(static a => a.Organization).FirstOrDefaultAsync(a => a.Slug == request.Slug, cancellationToken);
            var canManage = article is not null && NewsDesk.CanManage(article, await desk.ManageScopeAsync(cancellationToken));
            if (article is null || (!article.IsPublished && !canManage))
            {
                return NewsDesk.NotFound();
            }

            return desk.ToDto(article, canManage, await db.NewsViews.CountAsync(v => v.ArticleId == article.Id, cancellationToken));
        }
    }

    /// <summary>
    /// Reads an article for editing when it is in the scope.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared news rules.</param>
    internal sealed class ForEditingHandler(ApplicationDbContext db, NewsDesk desk) : IRequestHandler<ForEditingQuery, Result<NewsArticleDto>>
    {
        /// <inheritdoc />
        public async Task<Result<NewsArticleDto>> HandleAsync(ForEditingQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).Include(static a => a.Organization).FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (article is null)
            {
                return NewsDesk.NotFound();
            }

            return NewsDesk.CanManage(article, await desk.ManageScopeAsync(cancellationToken))
                ? desk.ToDto(article, canManage: true, await db.NewsViews.CountAsync(v => v.ArticleId == article.Id, cancellationToken))
                : Error.Forbidden(Texts.T(NewsDesk.OutsideScopeMessage));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/news/{slug}</c> and <c>GET /api/v1/news/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var news = NewsDesk.MapNews(api);
            news.MapGet("{slug}", BySlugAsync);
            news.MapGet("{id:guid}", ForEditingAsync).RequirePermission(Permissions.News.Manage);
        }

        /// <summary>
        /// Gets an article by its slug.
        /// </summary>
        /// <param name="slug">The address of the article.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The article or the problem.</returns>
        private static async Task<Results<Ok<NewsArticleDto>, ProblemHttpResult>> BySlugAsync(string slug, IRequestHandler<BySlugQuery, Result<NewsArticleDto>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new BySlugQuery(slug), cancellationToken)).ToOk();

        /// <summary>
        /// Gets an article for editing (drafts included) by its identifier.
        /// </summary>
        /// <param name="id">The article.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The article or the problem.</returns>
        private static async Task<Results<Ok<NewsArticleDto>, ProblemHttpResult>> ForEditingAsync(Guid id, IRequestHandler<ForEditingQuery, Result<NewsArticleDto>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new ForEditingQuery(id), cancellationToken)).ToOk();
    }
}
