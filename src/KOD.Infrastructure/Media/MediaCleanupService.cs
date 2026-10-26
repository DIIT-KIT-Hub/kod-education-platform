using KOD.Application.Abstractions;
using KOD.Domain.Media;
using KOD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KOD.Infrastructure.Media;

/// <summary>
/// Periodically deletes images nobody uses: news images not attached to an article (uploaded into an editor that was
/// never saved, or left by a deleted article) and replaced or abandoned profile photos, a day after the upload.
/// </summary>
/// <param name="scopeFactory">Creates scopes for the database context.</param>
/// <param name="storage">The image storage.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class MediaCleanupService(
    IServiceScopeFactory scopeFactory,
    IMediaStorage storage,
    TimeProvider timeProvider,
    ILogger<MediaCleanupService> logger) : BackgroundService
{
    /// <summary>
    /// The cleanup period.
    /// </summary>
    private static readonly TimeSpan Period = TimeSpan.FromHours(6);

    /// <summary>
    /// How long an unused image is kept (an editor may still be open).
    /// </summary>
    private static readonly TimeSpan Grace = TimeSpan.FromDays(1);

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
                var before = timeProvider.GetUtcNow() - Grace;
                var unused = await db.MediaFiles
                    .Where(m => m.CreatedAt < before)
                    .Where(m => m.Purpose == MediaPurpose.News
                        ? m.NewsArticleId == null
                        : !db.Users.Any(u => u.PhotoId == m.Id))
                    .Select(static m => m.Id)
                    .Take(500)
                    .ToListAsync(stoppingToken);
                if (unused.Count > 0)
                {
                    await db.MediaFiles.Where(m => unused.Contains(m.Id)).ExecuteDeleteAsync(stoppingToken);
                    unused.ForEach(storage.Delete);
                    LogCleaned(logger, unused.Count);
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
    /// Logs the number of deleted images.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of deleted images.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} unused images")]
    private static partial void LogCleaned(ILogger logger, int count);

    /// <summary>
    /// Logs a cleanup failure.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Unused image cleanup failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
