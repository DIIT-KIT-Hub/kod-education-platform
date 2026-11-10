using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;

namespace ODC.Web.Features.News;

/// <summary>
/// Deletes an article with its images and discussion.
/// </summary>
public static class DeleteNewsArticle
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The article.</param>
    [RequirePermission(Permissions.News.Manage)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Deletes the article when it is in the scope; the image files go after the save.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared news rules.</param>
    /// <param name="media">The image library.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(OrganizationDbContext db, NewsDesk desk, MediaLibrary media, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (article is null)
            {
                return NewsDesk.NotFound();
            }

            if (!NewsDesk.CanManage(article, await desk.ManageScopeAsync(cancellationToken)))
            {
                return Error.Forbidden(Texts.T(NewsDesk.OutsideScopeMessage));
            }

            foreach (var image in await media.Files.Where(m => m.NewsArticleId == request.Id).ToListAsync(cancellationToken))
            {
                media.Remove(image);
            }

            db.NewsArticles.Remove(article);
            audit.Add(AuditActions.NewsDeleted, nameof(NewsArticle), article.Id.ToString(), $"Видалено «{article.Title}»");
            await db.SaveChangesAsync(cancellationToken);
            await media.SaveChangesAsync(cancellationToken);
            await desk.IndexAsync(article, deleted: true, cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/news/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => NewsDesk.MapNews(api).MapDelete("{id:guid}", HandleAsync).RequirePermission(Permissions.News.Manage);

        /// <summary>
        /// Deletes an article.
        /// </summary>
        /// <param name="id">The article.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
