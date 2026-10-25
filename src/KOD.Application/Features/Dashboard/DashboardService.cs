using KOD.Application.Abstractions;
using KOD.Application.Features.Documents;
using KOD.Application.Features.News;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Dashboard;

/// <summary>
/// Default <see cref="IDashboardService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="documents">The document service.</param>
/// <param name="news">The news service.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class DashboardService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IDocumentService documents,
    INewsService news,
    TimeProvider timeProvider) : IDashboardService
{
    /// <inheritdoc />
    public async Task<DashboardSummary> GetAsync(CancellationToken cancellationToken)
    {
        var awaiting = new List<DocumentListItem>();
        int awaitingCount = 0, drafts = 0, inSigning = 0, signedThisMonth = 0;

        if (currentUser.Id is { } me && currentUser.Has(Permissions.Documents.View))
        {
            var page = await documents.ListAsync(new DocumentQuery { Scope = DocumentScope.AwaitingMe, PageSize = 5 }, cancellationToken);
            if (page.IsSuccess)
            {
                awaiting.AddRange(page.Value.Items);
                awaitingCount = page.Value.TotalCount;
            }

            var now = timeProvider.GetUtcNow();
            var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var mine = db.Documents.AsNoTracking().Where(d => d.AuthorId == me);
            drafts = await mine.CountAsync(static d => d.Status == DocumentStatus.Draft, cancellationToken);
            inSigning = await mine.CountAsync(static d => d.Status == DocumentStatus.InSigning, cancellationToken);
            signedThisMonth = await mine.CountAsync(d => d.Status == DocumentStatus.Signed && d.CompletedAt >= monthStart, cancellationToken);
        }

        var latestNews = currentUser.Has(Permissions.News.View)
            ? await news.ListAsync(new NewsQuery { PageSize = 3 }, cancellationToken)
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
