using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// Who may read an article's discussion and what they may do in it: readers of news see the discussions of published
/// articles, the article's news managers also of drafts and may delete any comment.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The administration scopes.</param>
internal sealed class DiscussionAccess(OrganizationDbContext db, ICurrentUser currentUser, IAccessScope accessScope)
{
    /// <summary>
    /// The default number of threads per page.
    /// </summary>
    public const int PageSize = 10;

    /// <summary>
    /// The largest page a client may ask for.
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    /// The route group <c>/api/v1/news/{articleId}</c> of the discussion endpoints (readers of news).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapDiscussion(IEndpointRouteBuilder api)
        => api.MapGroup("news/{articleId:guid}").RequirePermission(Permissions.News.View).WithTags("NewsDiscussion");

    /// <summary>
    /// The error of an article the user cannot read.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error ArticleNotFound() => Error.NotFound(Texts.T("Новину не знайдено."));

    /// <summary>
    /// The error of a missing or deleted comment.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error CommentNotFound() => Error.NotFound(Texts.T("Коментар не знайдено."));

    /// <summary>
    /// Checks that the current user may read an article and returns what they may do with its discussion.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The permissions, or <see langword="null"/> when the article is not readable.</returns>
    public async Task<Rights?> OfAsync(Guid articleId, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.News.View))
        {
            return null;
        }

        var article = await db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);
        if (article is null)
        {
            return null;
        }

        var canManage = currentUser.Has(Permissions.News.Manage)
            && NewsDesk.CanManage(article, await accessScope.GetAdministrationScopeAsync(Permissions.News.Manage, cancellationToken));
        if (!article.IsPublished && !canManage)
        {
            return null;
        }

        return new Rights(article.IsPublished, article.IsPublished && article.AllowComments, canManage);
    }

    /// <summary>
    /// What the current user may do with a discussion.
    /// </summary>
    /// <param name="CanReact">Whether reactions are possible (the article is published).</param>
    /// <param name="CanComment">Whether comments are open.</param>
    /// <param name="CanModerate">Whether the user manages the article's news and may delete any comment.</param>
    public sealed record Rights(bool CanReact, bool CanComment, bool CanModerate);
}
