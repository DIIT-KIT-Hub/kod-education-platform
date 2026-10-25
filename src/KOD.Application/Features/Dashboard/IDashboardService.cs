namespace KOD.Application.Features.Dashboard;

/// <summary>
/// Builds the home page summary.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Builds the summary for the current user (sections the user has no access to are empty).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The summary.</returns>
    Task<DashboardSummary> GetAsync(CancellationToken cancellationToken);
}
