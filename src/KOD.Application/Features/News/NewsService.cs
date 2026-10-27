using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Media;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Application.Features.Media;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Media;
using KOD.Domain.News;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.News;

/// <summary>
/// Default <see cref="INewsService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data access rules of the current user.</param>
/// <param name="markdown">The Markdown renderer (articles written before the rich text editor).</param>
/// <param name="sanitizer">The rich text sanitizer.</param>
/// <param name="media">The image library.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class NewsService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAccessScope accessScope,
    IMarkdownRenderer markdown,
    IRichTextSanitizer sanitizer,
    MediaLibrary media,
    IAuditLogger audit,
    TimeProvider timeProvider) : INewsService
{
    /// <summary>
    /// The error shown when an article belongs to an organization the current user does not manage the news of.
    /// </summary>
    private const string OutsideScopeMessage = "Ця новина належить організації, новинами якої ви не керуєте.";

    /// <inheritdoc />
    public async Task<Result<PagedResult<NewsListItem>>> ListAsync(NewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.News.View))
        {
            return Error.Forbidden();
        }

        // Everyone reads the published news of every organization; the management view lists only the articles the user manages.
        var articles = db.NewsArticles.AsNoTracking();
        if (query.IncludeDrafts && currentUser.Has(Permissions.News.Manage))
        {
            articles = Manageable(articles, await ManageScopeAsync(cancellationToken));
        }
        else
        {
            articles = articles.Where(static a => a.PublishedAt != null);
        }

        if (query.Category is { } category)
        {
            articles = articles.Where(a => a.Category == category);
        }

        if (query.Search is { } search)
        {
            var term = search;
            articles = articles.Where(a => a.Title.Contains(term) || a.Summary.Contains(term));
        }

        articles = query.SortBy switch
        {
            "title" => articles.OrderByDirection(static a => a.Title, query.Descending),
            "updated" => articles.OrderByDirection(static a => a.UpdatedAt ?? a.CreatedAt, query.Descending),
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
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<NewsArticleDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.View))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).Include(static a => a.Organization).FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
        var canManage = article is not null && CanManage(article, await ManageScopeAsync(cancellationToken));
        if (article is null || (!article.IsPublished && !canManage))
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        return ToDto(article, canManage, await db.NewsViews.CountAsync(v => v.ArticleId == article.Id, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<NewsArticleDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).Include(static a => a.Organization).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        return CanManage(article, await ManageScopeAsync(cancellationToken))
            ? ToDto(article, canManage: true, await db.NewsViews.CountAsync(v => v.ArticleId == article.Id, cancellationToken))
            : Error.Forbidden(Texts.T(OutsideScopeMessage));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(SaveNewsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me || !currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var organizationError = await ValidateOrganizationAsync(command.OrganizationId, await ManageScopeAsync(cancellationToken), cancellationToken);
        if (organizationError is not null)
        {
            return organizationError;
        }

        var html = sanitizer.Sanitize(command.Body);
        if (sanitizer.IsEmpty(html))
        {
            return Error.Validation(Texts.T("Напишіть текст новини."));
        }

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var slug = await UniqueSlugAsync(command.Title, cancellationToken);
            var article = new NewsArticle(slug, command.Title, command.Summary, html, NewsBodyFormat.Html, command.Category, me);
            article.Update(command.Title, command.Summary, html, command.Category, command.IsPinned, command.AllowComments);
            article.AssignTo(command.OrganizationId);

            var coverError = await ApplyCoverAsync(article, command, me, cancellationToken);
            if (coverError is not null)
            {
                return coverError;
            }

            if (command.Publish)
            {
                article.Publish(timeProvider.GetUtcNow());
            }

            db.NewsArticles.Add(article);
            await SyncImagesAsync(article, cancellationToken);
            audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Створено «{article.Title}»");
            await media.SaveChangesAsync(cancellationToken);
            return article.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveNewsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me || !currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        var scope = await ManageScopeAsync(cancellationToken);
        if (!CanManage(article, scope))
        {
            return Error.Forbidden(Texts.T(OutsideScopeMessage));
        }

        var organizationError = await ValidateOrganizationAsync(command.OrganizationId, scope, cancellationToken);
        if (organizationError is not null)
        {
            return organizationError;
        }

        var html = sanitizer.Sanitize(command.Body);
        if (sanitizer.IsEmpty(html))
        {
            return Error.Validation(Texts.T("Напишіть текст новини."));
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            article.Update(command.Title, command.Summary, html, command.Category, command.IsPinned, command.AllowComments);
            article.AssignTo(command.OrganizationId);
            var coverError = await ApplyCoverAsync(article, command, me, cancellationToken);
            if (coverError is not null)
            {
                return coverError;
            }

            if (command.Publish)
            {
                article.Publish(timeProvider.GetUtcNow());
            }
            else
            {
                article.Unpublish();
            }

            await SyncImagesAsync(article, cancellationToken);
            audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Змінено «{article.Title}»");
            await media.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return Error.NotFound(Texts.T("Новину не знайдено."));
        }

        if (!CanManage(article, await ManageScopeAsync(cancellationToken)))
        {
            return Error.Forbidden(Texts.T(OutsideScopeMessage));
        }

        foreach (var image in await db.MediaFiles.Where(m => m.NewsArticleId == id).ToListAsync(cancellationToken))
        {
            media.Remove(image);
        }

        db.NewsArticles.Remove(article);
        audit.Add(AuditActions.NewsDeleted, nameof(NewsArticle), article.Id.ToString(), $"Видалено «{article.Title}»");
        await media.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task RecordViewAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me || await db.NewsViews.AnyAsync(v => v.ArticleId == id && v.UserId == me, cancellationToken))
        {
            return;
        }

        db.NewsViews.Add(new NewsView(id, me, timeProvider.GetUtcNow()));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The same reader opened the article in another tab at the same moment: the view is already counted.
            db.DiscardChanges();
        }
    }

    /// <summary>
    /// Determines whether an article is in the scope; news of the whole cabinet belong only to the cabinet-wide scope.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <returns><see langword="true"/> when the article may be managed.</returns>
    internal static bool CanManage(NewsArticle article, AdministrationScope scope)
        => scope.IsCabinetWide || (article.OrganizationId is { } organizationId && scope.Contains(organizationId));

    /// <summary>
    /// Restricts a query to the articles of the scope: every article in the cabinet-wide scope, otherwise the articles of its organizations.
    /// </summary>
    /// <param name="articles">The query.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <returns>The restricted query.</returns>
    private static IQueryable<NewsArticle> Manageable(IQueryable<NewsArticle> articles, AdministrationScope scope)
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
    private Task<AdministrationScope> ManageScopeAsync(CancellationToken cancellationToken)
        => accessScope.GetAdministrationScopeAsync(Permissions.News.Manage, cancellationToken);

    /// <summary>
    /// Validates the organization of an article: an organization in the scope, or none (the whole cabinet) for the cabinet-wide scope.
    /// </summary>
    /// <param name="organizationId">The requested organization.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    private async Task<Error?> ValidateOrganizationAsync(Guid? organizationId, AdministrationScope scope, CancellationToken cancellationToken)
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
    /// <param name="command">The command.</param>
    /// <param name="me">The current user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error when the image is invalid; otherwise <see langword="null"/>.</returns>
    private async Task<Error?> ApplyCoverAsync(NewsArticle article, SaveNewsCommand command, Guid me, CancellationToken cancellationToken)
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
    private async Task SyncImagesAsync(NewsArticle article, CancellationToken cancellationToken)
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
    private async Task<string> UniqueSlugAsync(string title, CancellationToken cancellationToken)
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
    private NewsArticleDto ToDto(NewsArticle article, bool canManage, int viewCount) => new(
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
