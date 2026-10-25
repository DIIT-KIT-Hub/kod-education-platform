using KOD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Periodically deletes expired server-side sessions.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the database context.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class SessionCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<SessionCleanupService> logger) : BackgroundService
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
                var deleted = await db.UserSessions.Where(s => s.ExpiresAt < now).ExecuteDeleteAsync(stoppingToken);
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
    /// Logs the number of deleted sessions.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of deleted sessions.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} expired sessions")]
    private static partial void LogCleaned(ILogger logger, int count);

    /// <summary>
    /// Logs a cleanup failure.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Expired session cleanup failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
