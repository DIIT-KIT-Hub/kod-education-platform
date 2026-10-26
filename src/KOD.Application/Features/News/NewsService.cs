using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.News;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.News;

/// <summary>
/// Default <see cref="INewsService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data access rules of the current user.</param>
/// <param name="markdown">The Markdown renderer.</param>
/// <param name="assets">The public asset storage.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class NewsService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAccessScope accessScope,
    IMarkdownRenderer markdown,
    IPublicAssetStorage assets,
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
            .Select(static a => new NewsListItem(
                a.Id,
                a.Slug,
                a.Title,
                a.Summary,
                a.Category,
                a.CoverImageUrl,
                a.IsPinned,
                a.PublishedAt,
                a.Author!.LastName + " " + a.Author.FirstName,
                a.UpdatedAt ?? a.CreatedAt,
                a.Organization != null ? new UnitRef(a.Organization.Id, a.Organization.Name) : null))
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
        return article is null || (!article.IsPublished && !canManage)
            ? Error.NotFound(Texts.T("Новину не знайдено."))
            : ToDto(article, canManage);
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

        return CanManage(article, await ManageScopeAsync(cancellationToken)) ? ToDto(article, canManage: true) : Error.Forbidden(Texts.T(OutsideScopeMessage));
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

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var slug = await UniqueSlugAsync(command.Title, cancellationToken);
            var article = new NewsArticle(slug, command.Title, command.Summary, command.Body, command.Category, me);
            article.Update(command.Title, command.Summary, command.Body, command.Category, command.IsPinned);
            article.AssignTo(command.OrganizationId);

            var coverError = await ApplyCoverAsync(article, command, cancellationToken);
            if (coverError is not null)
            {
                return coverError;
            }

            if (command.Publish)
            {
                article.Publish(timeProvider.GetUtcNow());
            }

            db.NewsArticles.Add(article);
            audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Створено «{article.Title}»");
            await db.SaveChangesAsync(cancellationToken);
            return article.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveNewsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.News.Manage))
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

        return await DomainErrors.GuardAsync(async () =>
        {
            article.Update(command.Title, command.Summary, command.Body, command.Category, command.IsPinned);
            article.AssignTo(command.OrganizationId);
            var coverError = await ApplyCoverAsync(article, command, cancellationToken);
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

            audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Змінено «{article.Title}»");
            await db.SaveChangesAsync(cancellationToken);
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

        db.NewsArticles.Remove(article);
        audit.Add(AuditActions.NewsDeleted, nameof(NewsArticle), article.Id.ToString(), $"Видалено «{article.Title}»");
        await db.SaveChangesAsync(cancellationToken);
        if (article.CoverImageUrl is { } cover)
        {
            assets.Delete(cover);
        }

        return Result.Success();
    }

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
    /// Determines whether an article is in the scope; news of the whole cabinet belong only to the cabinet-wide scope.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="scope">The <c>news.manage</c> scope.</param>
    /// <returns><see langword="true"/> when the article may be managed.</returns>
    private static bool CanManage(NewsArticle article, AdministrationScope scope)
        => scope.IsCabinetWide || (article.OrganizationId is { } organizationId && scope.Contains(organizationId));

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
    /// Stores a new cover image or removes the existing one.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error when the image is invalid; otherwise <see langword="null"/>.</returns>
    private async Task<Error?> ApplyCoverAsync(NewsArticle article, SaveNewsCommand command, CancellationToken cancellationToken)
    {
        var previous = article.CoverImageUrl;
        if (command.CoverImage is { } image)
        {
            if (image.Length > FileRules.MaxImageSize)
            {
                return Error.Validation(Texts.T("Зображення не може бути більшим за 5 МБ."));
            }

            var url = await assets.SaveImageAsync(image.Content, "news", cancellationToken);
            if (url is null)
            {
                return Error.Validation(Texts.T("Підтримуються лише зображення PNG, JPEG або WebP."));
            }

            article.SetCover(url);
        }
        else if (command.RemoveCover)
        {
            article.SetCover(null);
        }

        if (previous is not null && previous != article.CoverImageUrl)
        {
            assets.Delete(previous);
        }

        return null;
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
    /// <returns>The DTO.</returns>
    private NewsArticleDto ToDto(NewsArticle article, bool canManage) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Body,
        markdown.ToHtml(article.Body),
        article.Category,
        article.CoverImageUrl,
        article.IsPinned,
        article.PublishedAt,
        article.Author?.FullName ?? string.Empty,
        article.UpdatedAt ?? article.CreatedAt,
        article.Organization is { } organization ? new UnitRef(organization.Id, organization.Name) : null,
        canManage);
}
