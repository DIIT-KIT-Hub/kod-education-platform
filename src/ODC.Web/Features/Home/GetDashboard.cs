using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
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
/// their public queries; the documents and their counts from every organization the user works in or takes part in
/// (<see cref="IAcrossOrganizations"/>: the documents query runs in each of them).
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
    /// <param name="across">Reads the user's organizations.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="news">Lists news.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        IAcrossOrganizations across,
        ICurrentUser currentUser,
        IRequestHandler<ListNews.Query, Result<PagedResult<NewsListItem>>> news,
        TimeProvider timeProvider) : IRequestHandler<Query, DashboardSummary>
    {
        /// <inheritdoc />
        public async Task<DashboardSummary> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var awaiting = new List<DashboardDocument>();
            int awaitingCount = 0, drafts = 0, inSigning = 0, signedThisMonth = 0;

            if (currentUser.Id is { } me && currentUser.Has(Permissions.Documents.View))
            {
                var now = timeProvider.GetUtcNow();
                var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, KyivTime.Zone).DateTime);
                var monthStart = KyivTime.StartOfDay(new DateOnly(today.Year, today.Month, 1));
                var parts = await across.GatherInAsync(
                    me,
                    async services =>
                    {
                        var documents = services.GetRequiredService<IRequestHandler<DocumentQuery, Result<PagedResult<DocumentListItem>>>>();
                        var page = await documents.HandleAsync(new DocumentQuery { Scope = DocumentScope.AwaitingMe, PageSize = 5 }, cancellationToken);
                        var db = services.GetRequiredService<OrganizationDbContext>();
                        var mine = db.Documents.AsNoTracking().Where(d => d.AuthorId == me);
                        return new Figures(
                            page.IsSuccess ? page.Value.Items : [],
                            page.IsSuccess ? page.Value.TotalCount : 0,
                            await mine.CountAsync(static d => d.Status == DocumentStatus.Draft, cancellationToken),
                            await mine.CountAsync(static d => d.Status == DocumentStatus.InSigning, cancellationToken),
                            await db.Documents.AsNoTracking()
                                .CountAsync(d => d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Signed && s.DecidedAt >= monthStart), cancellationToken));
                    },
                    cancellationToken);

                awaiting.AddRange(parts
                    .SelectMany(static p => p.Value.Awaiting.Select(d => new DashboardDocument(d, p.Organization.Id, p.Organization.Code)))
                    .OrderByDescending(static d => d.Document.CreatedAt)
                    .Take(5));
                awaitingCount = parts.Sum(static p => p.Value.AwaitingCount);
                drafts = parts.Sum(static p => p.Value.Drafts);
                inSigning = parts.Sum(static p => p.Value.InSigning);
                signedThisMonth = parts.Sum(static p => p.Value.SignedThisMonth);
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
    /// What one organization adds to the home page.
    /// </summary>
    /// <param name="Awaiting">The newest documents waiting for the user there.</param>
    /// <param name="AwaitingCount">How many wait there.</param>
    /// <param name="Drafts">The user's drafts there.</param>
    /// <param name="InSigning">The user's documents on signing routes there.</param>
    /// <param name="SignedThisMonth">The documents the user signed there this month.</param>
    private sealed record Figures(IReadOnlyList<DocumentListItem> Awaiting, int AwaitingCount, int Drafts, int InSigning, int SignedThisMonth);

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
