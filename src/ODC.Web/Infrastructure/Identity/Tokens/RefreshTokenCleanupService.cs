using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;

namespace ODC.Web.Infrastructure.Identity.Tokens;

/// <summary>
/// Periodically deletes expired refresh token families (idle timeout or absolute lifetime passed) and activation code
/// requests older than a day.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the database context.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    /// <summary>
    /// The cleanup period.
    /// </summary>
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = timeProvider.GetUtcNow();
                var deleted = await db.RefreshTokenFamilies.Where(f => f.ExpiresAt <= now).ExecuteDeleteAsync(stoppingToken);

                // Activation code requests are counted for the hourly limit, then kept no longer than a day.
                var dayAgo = now.AddDays(-1);
                deleted += await db.ActivationChallenges.Where(c => c.CreatedAt <= dayAgo).ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                {
                    LogCleaned(logger, deleted);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Logs the number of deleted families.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of deleted families.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} expired refresh token families and activation code requests")]
    private static partial void LogCleaned(ILogger logger, int count);

    /// <summary>
    /// Logs a cleanup failure.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Expired refresh token cleanup failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
