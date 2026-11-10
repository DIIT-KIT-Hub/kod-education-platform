using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.News;

/// <summary>
/// Replaces an article (text, organization, cover, publication); the cover can also be replaced on its own.
/// </summary>
public static class UpdateNewsArticle
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The article.</param>
    /// <param name="Article">The new data.</param>
    [RequirePermission(Permissions.News.Manage)]
    public sealed record Command(Guid Id, SaveNewsCommand Article) : ICommand<Result>;

    /// <summary>
    /// Validates the scope, the organization and the text and saves the article with its images.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared news rules.</param>
    /// <param name="sanitizer">The rich text sanitizer.</param>
    /// <param name="media">The image library.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        OrganizationDbContext db,
        ICurrentUser currentUser,
        NewsDesk desk,
        IRichTextSanitizer sanitizer,
        MediaLibrary media,
        IAuditLogger audit,
        TimeProvider timeProvider) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request.Article;
            if (currentUser.Id is not { } me)
            {
                return Error.Forbidden();
            }

            var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (article is null)
            {
                return NewsDesk.NotFound();
            }

            var scope = await desk.ManageScopeAsync(cancellationToken);
            if (!NewsDesk.CanManage(article, scope))
            {
                return Error.Forbidden(Texts.T(NewsDesk.OutsideScopeMessage));
            }

            if (await desk.ValidateOrganizationAsync(command.OrganizationId, scope, cancellationToken) is { } organizationError)
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
                if (await desk.ApplyCoverAsync(article, command, me, cancellationToken) is { } coverError)
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

                await desk.SyncImagesAsync(article, cancellationToken);
                audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Змінено «{article.Title}»");
                await db.SaveChangesAsync(cancellationToken);
                await media.SaveChangesAsync(cancellationToken);
                await desk.IndexAsync(article, deleted: false, cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/news/{id}</c> and <c>PUT /api/v1/news/{id}/cover</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var news = NewsDesk.MapNews(api);
            news.MapPut("{id:guid}", HandleAsync).RequirePermission(Permissions.News.Manage);
            news.MapPut("{id:guid}/cover", CoverAsync)
                .RequirePermission(Permissions.News.Manage)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxImageSize + (64 * 1024)));
        }

        /// <summary>
        /// Replaces an article.
        /// </summary>
        /// <param name="id">The article.</param>
        /// <param name="article">The new data.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            NewsRequest article,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, article.ToCommand()), cancellationToken)).ToNoContent();

        /// <summary>
        /// Replaces the cover image (JPEG, PNG, WebP, AVIF or GIF up to 15 MB, stored as AVIF).
        /// </summary>
        /// <param name="id">The article.</param>
        /// <param name="file">The image.</param>
        /// <param name="current">Reads the article as it is.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> CoverAsync(
            Guid id,
            IFormFile file,
            IRequestHandler<GetNewsArticle.ForEditingQuery, Result<NewsArticleDto>> current,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
        {
            var existing = await current.HandleAsync(new GetNewsArticle.ForEditingQuery(id), cancellationToken);
            if (!existing.IsSuccess)
            {
                return existing.Error.ToProblem();
            }

            await using var content = file.OpenReadStream();
            var article = existing.Value;
            var command = new SaveNewsCommand(
                article.Title,
                article.Summary,
                article.BodyHtml,
                article.Category,
                article.IsPinned,
                article.PublishedAt is not null,
                new FileUpload(content, file.FileName, file.ContentType, file.Length),
                RemoveCover: false,
                article.Organization?.Id,
                article.AllowComments);
            return (await handler.HandleAsync(new Command(id, command), cancellationToken)).ToNoContent();
        }
    }
}
