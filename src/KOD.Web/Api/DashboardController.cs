using KOD.Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The home page data of the signed-in user.
/// </summary>
/// <param name="dashboard">The dashboard service.</param>
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    /// <summary>
    /// Gets the key figures, the documents waiting for the user's signature and the latest news (sections the user has no
    /// access to are empty).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The summary.</returns>
    [HttpGet]
    public async Task<ActionResult<DashboardSummary>> Get(CancellationToken cancellationToken)
        => await dashboard.GetAsync(cancellationToken);
}
