using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Home;

/// <summary>
/// The home page of the signed-in user: key figures, the documents waiting for the user's signature and the latest
/// news (sections the user has no access to are empty). The lists come from the documents and news features through
/// their public queries.
/// </summary>
public static class GetDashboard
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Query : IQuery<DashboardSummary>;

    /// <summary>
    /// Builds the summary.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="documents">Lists documents.</param>
    /// <param name="news">Lists news.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        OrganizationDbContext db,
        ICurrentUser currentUser,
        IRequestHandler<DocumentQuery, Result<PagedResult<DocumentListItem>>> documents,
        IRequestHandler<ListNews.Query, Result<PagedResult<NewsListItem>>> news,
        TimeProvider timeProvider) : IRequestHandler<Query, DashboardSummary>
    {
        /// <inheritdoc />
        public async Task<DashboardSummary> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var awaiting = new List<DocumentListItem>();
            int awaitingCount = 0, drafts = 0, inSigning = 0, signedThisMonth = 0;

            if (currentUser.Id is { } me && currentUser.Has(Permissions.Documents.View))
            {
                var page = await documents.HandleAsync(new DocumentQuery { Scope = DocumentScope.AwaitingMe, PageSize = 5 }, cancellationToken);
                if (page.IsSuccess)
                {
                    awaiting.AddRange(page.Value.Items);
                    awaitingCount = page.Value.TotalCount;
                }

                var now = timeProvider.GetUtcNow();
                var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, KyivTime.Zone).DateTime);
                var monthStart = KyivTime.StartOfDay(new DateOnly(today.Year, today.Month, 1));
                var mine = db.Documents.AsNoTracking().Where(d => d.AuthorId == me);
                drafts = await mine.CountAsync(static d => d.Status == DocumentStatus.Draft, cancellationToken);
                inSigning = await mine.CountAsync(static d => d.Status == DocumentStatus.InSigning, cancellationToken);
                signedThisMonth = await db.Documents.AsNoTracking()
                    .CountAsync(d => d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Signed && s.DecidedAt >= monthStart), cancellationToken);
            }

            var latestNews = currentUser.Has(Permissions.News.View)
                ? await news.HandleAsync(new ListNews.Query { PageSize = 3 }, cancellationToken)
                : null;

            return new DashboardSummary(
                awaitingCount,
                drafts,
                inSigning,
                signedThisMonth,
                awaiting,
                latestNews is { IsSuccess: true } ? latestNews.Value.Items : []);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/dashboard</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapGet("dashboard", HandleAsync).WithTags("Dashboard");

        /// <summary>
        /// Gets the key figures, the documents waiting for the user's signature and the latest news (sections the user has
        /// no access to are empty).
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The summary.</returns>
        private static async Task<Ok<DashboardSummary>> HandleAsync(IRequestHandler<Query, DashboardSummary> handler, CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(new Query(), cancellationToken));
    }
}
