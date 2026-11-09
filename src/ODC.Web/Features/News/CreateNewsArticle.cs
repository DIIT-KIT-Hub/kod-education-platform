using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.News;

/// <summary>
/// Creates an article; the HTML of the editor is sanitized (allow-list, images only from <c>/media/…</c>).
/// </summary>
public static class CreateNewsArticle
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Article">The article.</param>
    [RequirePermission(Permissions.News.Manage)]
    public sealed record Command(SaveNewsCommand Article) : ICommand<Result<Guid>>;

    /// <summary>
    /// Validates the organization and the text and saves the article with its images.
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
        TimeProvider timeProvider) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request.Article;
            if (currentUser.Id is not { } me)
            {
                return Error.Forbidden();
            }

            if (await desk.ValidateOrganizationAsync(command.OrganizationId, await desk.ManageScopeAsync(cancellationToken), cancellationToken) is { } organizationError)
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
                var slug = await desk.UniqueSlugAsync(command.Title, cancellationToken);
                var article = new NewsArticle(slug, command.Title, command.Summary, html, NewsBodyFormat.Html, command.Category, me);
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

                db.NewsArticles.Add(article);
                await desk.SyncImagesAsync(article, cancellationToken);
                audit.Add(AuditActions.NewsChanged, nameof(NewsArticle), article.Id.ToString(), $"Створено «{article.Title}»");
                await db.SaveChangesAsync(cancellationToken);
                await media.SaveChangesAsync(cancellationToken);
                return article.Id;
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/news</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => NewsDesk.MapNews(api).MapPost(string.Empty, HandleAsync).RequirePermission(Permissions.News.Manage);

        /// <summary>
        /// Creates an article.
        /// </summary>
        /// <param name="article">The article.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            NewsRequest article,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(article.ToCommand()), cancellationToken)).ToCreated(static id => $"{ApiEndpoints.Base}/news/{id}");
    }
}
