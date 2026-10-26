using KOD.Application.Features.Documents;
using KOD.Application.Features.News;

namespace KOD.Application.Features.Dashboard;

/// <summary>
/// Data of the home page.
/// </summary>
/// <param name="AwaitingMyDecision">The number of documents waiting for the user's signature.</param>
/// <param name="MyDrafts">The number of the user's drafts.</param>
/// <param name="MyInSigning">The number of the user's documents on signing routes.</param>
/// <param name="SignedThisMonth">The number of documents the user signed this month.</param>
/// <param name="Awaiting">The newest documents waiting for the user's signature.</param>
/// <param name="LatestNews">The latest published news.</param>
public sealed record DashboardSummary(
    int AwaitingMyDecision,
    int MyDrafts,
    int MyInSigning,
    int SignedThisMonth,
    IReadOnlyList<DocumentListItem> Awaiting,
    IReadOnlyList<NewsListItem> LatestNews);
