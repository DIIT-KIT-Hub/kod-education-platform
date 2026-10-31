using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace KOD.Web.Hosting;

/// <summary>
/// Serves the single-page application from the same origin as the API, in development and in production alike: its
/// static files (hashed bundles cached for a year, everything else revalidated) and <c>index.html</c> for every
/// client-side route.
/// </summary>
internal static partial class SpaHostingExtensions
{
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        /// <summary>
        /// Adds the static file middleware of the application folder.
        /// </summary>
        /// <returns>The web application.</returns>
        public WebApplication UseSpaFiles()
        {
            if (app.SpaFiles() is { } files)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = files,
                    RequestPath = SpaOptions.BasePath,
                    OnPrepareResponse = static context => SetCacheHeaders(context.Context.Response, context.Context.Request.Path),
                });
            }

            return app;
        }

        /// <summary>
        /// Maps the client-side routes to <c>index.html</c>; the application itself handles sign-in and permissions.
        /// </summary>
        /// <returns>The web application.</returns>
        public WebApplication MapSpaFallback()
        {
            if (app.SpaFiles() is { } files)
            {
                app.MapFallbackToFile($"{SpaOptions.BasePath}/{{*path:nonfile}}", "index.html", new StaticFileOptions
                {
                    FileProvider = files,
                    OnPrepareResponse = static context => context.Context.Response.Headers.CacheControl = "no-cache",
                })
                .AllowAnonymous();
            }

            return app;
        }

        /// <summary>
        /// Opens the application folder.
        /// </summary>
        /// <returns>The file provider, or <see langword="null"/> when the application has not been built.</returns>
        private PhysicalFileProvider? SpaFiles()
        {
            var options = app.Services.GetRequiredService<IOptions<SpaOptions>>().Value;
            var root = Path.GetFullPath(options.Root, app.Environment.ContentRootPath);
            if (Directory.Exists(root))
            {
                return new PhysicalFileProvider(root);
            }

            LogSpaMissing(app.Logger, root);
            return null;
        }
    }

    /// <summary>
    /// Bundles with a content hash in the name never change: cache them for a year; revalidate the rest.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="path">The request path.</param>
    private static void SetCacheHeaders(HttpResponse response, PathString path)
        => response.Headers.CacheControl = path.StartsWithSegments($"{SpaOptions.BasePath}/assets", StringComparison.OrdinalIgnoreCase)
            ? "public, max-age=31536000, immutable"
            : CacheControlHeaderValue.NoCacheString;

    /// <summary>
    /// Logs a missing application build.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="root">The expected folder.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The client application is not built ({Root}); run `pnpm dev` or `pnpm build` in src/KOD.Client")]
    private static partial void LogSpaMissing(ILogger logger, string root);
}
