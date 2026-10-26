using System.Globalization;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace KOD.Web.Hosting;

/// <summary>
/// Configures Serilog: the console, a daily rolling file and the <c>logs</c> table of the application database.
/// </summary>
internal static class LoggingExtensions
{
    /// <param name="builder">The web application builder.</param>
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Replaces the default logging providers with Serilog.
        /// </summary>
        /// <returns>The builder.</returns>
        public WebApplicationBuilder AddApplicationLogging()
        {
            var options = builder.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();
            var filePath = Path.GetFullPath(options.FilePath, builder.Environment.ContentRootPath);
            var connectionString = builder.Configuration.GetConnectionString("Database");

            builder.Services.AddSerilog((services, logger) =>
            {
                logger
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "KOD")
                    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.File(
                        filePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: options.RetainedFiles,
                        formatProvider: CultureInfo.InvariantCulture,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    logger.WriteTo.PostgreSQL(
                        connectionString,
                        options.DatabaseTable,
                        new Dictionary<string, ColumnWriterBase>
                        {
                            ["timestamp"] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
                            ["level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
                            ["message"] = new RenderedMessageColumnWriter(NpgsqlDbType.Text),
                            ["message_template"] = new MessageTemplateColumnWriter(NpgsqlDbType.Text),
                            ["exception"] = new ExceptionColumnWriter(NpgsqlDbType.Text),
                            ["properties"] = new PropertiesColumnWriter(NpgsqlDbType.Jsonb),
                        },
                        formatProvider: CultureInfo.InvariantCulture,
                        needAutoCreateTable: true,
                        retentionTime: TimeSpan.FromDays(options.DatabaseRetentionDays));
                }
            });

            return builder;
        }
    }

    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        /// <summary>
        /// Logs one line per HTTP request. Static files and the Blazor connection are logged at the Debug level, so the
        /// Information log shows pages and API calls; slow requests are warnings and failures errors.
        /// </summary>
        /// <returns>The web application.</returns>
        public WebApplication UseRequestLogging()
        {
            app.UseSerilogRequestLogging(static options =>
            {
                options.GetLevel = static (context, elapsed, exception) => LevelOf(context, elapsed, exception);
                options.EnrichDiagnosticContext = static (diagnostics, context) =>
                {
                    diagnostics.Set("ClientIp", context.Connection.RemoteIpAddress?.ToString());
                    diagnostics.Set("UserId", context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                };
            });

            return app;
        }
    }

    /// <summary>
    /// Chooses the level of a request log line.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="elapsed">The duration in milliseconds.</param>
    /// <param name="exception">The unhandled exception, if any.</param>
    /// <returns>The level.</returns>
    private static LogEventLevel LevelOf(HttpContext context, double elapsed, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (elapsed > 3000)
        {
            return LogEventLevel.Warning;
        }

        return IsInfrastructure(context.Request.Path) ? LogEventLevel.Debug : LogEventLevel.Information;
    }

    /// <summary>
    /// Determines whether a request is for a static asset or the Blazor circuit rather than a page or the API.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns><see langword="true"/> for static files and framework requests.</returns>
    private static bool IsInfrastructure(PathString path)
        => path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/media", StringComparison.OrdinalIgnoreCase)
            || Path.HasExtension(path.Value);
}
