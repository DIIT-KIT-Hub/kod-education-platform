using KOD.Application.Abstractions;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Files;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.News;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.News;

/// <summary>
/// Default <see cref="INewsService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="markdown">The Markdown renderer.</param>
/// <param name="assets">The public asset storage.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class NewsService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IMarkdownRenderer markdown,
    IPublicAssetStorage assets,
    IAuditLogger audit,
    TimeProvider timeProvider) : INewsService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<NewsListItem>>> ListAsync(NewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.News.View))
        {
            return Error.Forbidden();
        }

        var articles = db.NewsArticles.AsNoTracking();
        if (!(query.IncludeDrafts && currentUser.Has(Permissions.News.Manage)))
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
                a.UpdatedAt ?? a.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<NewsArticleDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.View))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
        return article is null || (!article.IsPublished && !currentUser.Has(Permissions.News.Manage))
            ? Error.NotFound("Новину не знайдено.")
            : ToDto(article);
    }

    /// <inheritdoc />
    public async Task<Result<NewsArticleDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        var article = await db.NewsArticles.AsNoTracking().Include(static a => a.Author).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return article is null ? Error.NotFound("Новину не знайдено.") : ToDto(article);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(SaveNewsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me || !currentUser.Has(Permissions.News.Manage))
        {
            return Error.Forbidden();
        }

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var slug = await UniqueSlugAsync(command.Title, cancellationToken);
            var article = new NewsArticle(slug, command.Title, command.Summary, command.Body, command.Category, me);
            article.Update(command.Title, command.Summary, command.Body, command.Category, command.IsPinned);

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
            return Error.NotFound("Новину не знайдено.");
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            article.Update(command.Title, command.Summary, command.Body, command.Category, command.IsPinned);
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
            return Error.NotFound("Новину не знайдено.");
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
                return Error.Validation("Зображення не може бути більшим за 5 МБ.");
            }

            var url = await assets.SaveImageAsync(image.Content, "news", cancellationToken);
            if (url is null)
            {
                return Error.Validation("Підтримуються лише зображення PNG, JPEG або WebP.");
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
    /// <param name="article">The article.</param>
    /// <returns>The DTO.</returns>
    private NewsArticleDto ToDto(NewsArticle article) => new(
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
        article.UpdatedAt ?? article.CreatedAt);
}
