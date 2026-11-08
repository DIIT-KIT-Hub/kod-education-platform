using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.News;

/// <summary>
/// What the news slices share: the <c>news.manage</c> scope (an organization's managers edit its news, a cabinet-wide
/// manager also the news of the whole cabinet), the cover and the images of an article, slugs and the DTO.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="accessScope">The administration scopes.</param>
/// <param name="markdown">The Markdown renderer (articles written before the rich text editor).</param>
/// <param name="media">The image library.</param>
internal sealed class NewsDesk(ApplicationDbContext db, IAccessScope accessScope, IMarkdownRenderer markdown, MediaLibrary media)
{
    /// <summary>
    /// The error shown when an article belongs to an organization the current user does not manage the news of.
    /// </summary>
    public const string OutsideScopeMessage = "Ця новина належить організації, новинами якої Ви не керуєте.";

    /// <summary>
    /// The route group <c>/api/v1/news</c> (readers of news).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapNews(IEndpointRouteBuilder api) => api.MapGroup("news").RequirePermission(Permissions.News.View).WithTags("News");

    /// <summary>
    /// The error of a missing article.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Новину не знайдено."));

    /// <summary>
    /// Determines whether an article is in the scope; news of the whole cabinet belong only to the cabinet-wide scope.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <returns><see langword="true"/> when the article may be managed.</returns>
    public static bool CanManage(NewsArticle article, AdministrationScope scope)
        => scope.IsCabinetWide || (article.OrganizationId is { } organizationId && scope.Contains(organizationId));

    /// <summary>
    /// Restricts a query to the articles of the scope: every article in the cabinet-wide scope, otherwise the articles of
    /// its organizations.
    /// </summary>
    /// <param name="articles">The query.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <returns>The restricted query.</returns>
    public static IQueryable<NewsArticle> Manageable(IQueryable<NewsArticle> articles, AdministrationScope scope)
    {
        if (scope.Units is not { } scoped)
        {
            return articles;
        }

        var units = scoped.ToList();
        return articles.Where(a => a.OrganizationId != null && units.Contains(a.OrganizationId.Value));
    }

    /// <summary>
    /// Returns where <c>news.manage</c> of the current user applies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scope.</returns>
    public Task<AdministrationScope> ManageScopeAsync(CancellationToken cancellationToken)
        => accessScope.GetAdministrationScopeAsync(Permissions.News.Manage, cancellationToken);

    /// <summary>
    /// Validates the organization of an article: an organization in the scope, or none (the whole cabinet) for the
    /// cabinet-wide scope.
    /// </summary>
    /// <param name="organizationId">The requested organization.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    public async Task<Error?> ValidateOrganizationAsync(Guid? organizationId, AdministrationScope scope, CancellationToken cancellationToken)
    {
        if (organizationId is not { } id)
        {
            return scope.IsCabinetWide ? null : Error.Forbidden(Texts.T("Новини для всього кабінету публікує лише адміністратор усіх організацій. Оберіть свою організацію."));
        }

        if (!await db.OrganizationUnits.AnyAsync(u => u.Id == id && u.Type!.IsOrganization, cancellationToken))
        {
            return Error.Validation(Texts.T("Оберіть організацію новини."));
        }

        return scope.Contains(id) ? null : Error.Forbidden(Texts.T("Ви можете публікувати новини лише своїх організацій."));
    }

    /// <summary>
    /// Optimizes and sets a new cover image or removes the existing one (an uploaded cover is removed with the other
    /// unused images by <see cref="SyncImagesAsync"/>, an old public file right away).
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="command">The article data.</param>
    /// <param name="me">The current user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error when the image is invalid; otherwise <see langword="null"/>.</returns>
    public async Task<Error?> ApplyCoverAsync(NewsArticle article, SaveNewsCommand command, Guid me, CancellationToken cancellationToken)
    {
        if (command.CoverImage is { } image)
        {
            var added = await media.AddAsync(image, MediaPurpose.News, me, crop: null, cancellationToken);
            if (!added.IsSuccess)
            {
                return added.Error;
            }

            added.Value.AttachTo(article.Id);
            article.SetCover(MediaUrls.Of(added.Value.Id));
        }
        else if (command.RemoveCover)
        {
            article.SetCover(null);
        }

        return null;
    }

    /// <summary>
    /// Attaches the uploaded images the article uses (text, galleries, cover) and removes those it no longer uses.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the images are tracked.</returns>
    public async Task SyncImagesAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        var used = MediaUrls.IdsIn(article.Body).ToHashSet();
        if (MediaUrls.TryParse(article.CoverImageUrl, out var cover))
        {
            used.Add(cover);
        }

        var ids = used.ToList();
        var candidates = await db.MediaFiles
            .Where(m => m.Purpose == MediaPurpose.News && (m.NewsArticleId == article.Id || (m.NewsArticleId == null && ids.Contains(m.Id))))
            .ToListAsync(cancellationToken);
        foreach (var image in candidates)
        {
            if (used.Contains(image.Id))
            {
                image.AttachTo(article.Id);
            }
            else
            {
                media.Remove(image);
            }
        }
    }

    /// <summary>
    /// Generates a slug that is not used by another article.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique slug.</returns>
    public async Task<string> UniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = Slug.From(title ?? string.Empty, 120);
        var slug = baseSlug;
        for (var attempt = 2; await db.NewsArticles.AnyAsync(a => a.Slug == slug, cancellationToken); attempt++)
        {
            slug = $"{baseSlug}-{attempt}";
        }

        return slug;
    }

    /// <summary>
    /// Maps an entity to its DTO.
    /// </summary>
    /// <param name="article">The article with its author and organization.</param>
    /// <param name="canManage">Whether the current user may edit the article.</param>
    /// <param name="viewCount">How many readers opened the article.</param>
    /// <returns>The DTO.</returns>
    public NewsArticleDto ToDto(NewsArticle article, bool canManage, int viewCount) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Body,
        article.BodyFormat,
        article.BodyFormat == NewsBodyFormat.Html ? article.Body : markdown.ToHtml(article.Body),
        article.Category,
        article.CoverImageUrl,
        article.IsPinned,
        article.PublishedAt,
        new UserRef(article.AuthorId, article.Author?.FullName ?? string.Empty, article.Author?.PhotoId),
        article.UpdatedAt ?? article.CreatedAt,
        article.Organization is { } organization ? new UnitRef(organization.Id, organization.Name) : null,
        article.AllowComments,
        viewCount,
        canManage);
}
