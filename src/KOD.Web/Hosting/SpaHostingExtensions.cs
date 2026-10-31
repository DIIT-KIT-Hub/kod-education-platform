using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace KOD.Web.Hosting;

/// <summary>
/// Serves the single-page application at the root of the site, from the same origin as the API, in development and in
/// production alike: its static files (hashed bundles cached for a year, everything else revalidated) and
/// <c>index.html</c> for every client-side route.
/// </summary>
internal static partial class SpaHostingExtensions
{
    /// <summary>
    /// The path under which the client lived during the migration from the server-rendered pages; old links and
    /// bookmarks still work.
    /// </summary>
    private const string FormerBasePath = "/app";

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
                    OnPrepareResponse = static context => SetCacheHeaders(context.Context.Response, context.Context.Request.Path),
                });
            }

            return app;
        }

        /// <summary>
        /// Maps <c>index.html</c> as the fallback of every client-side route. Unknown API addresses stay 404 problem
        /// details, and the former <c>/app/…</c> addresses redirect to the same page at the root.
        /// </summary>
        /// <returns>The web application.</returns>
        public WebApplication MapSpaFallback()
        {
            app.Map("/api/{**path}", static () => Results.Problem(statusCode: StatusCodes.Status404NotFound))
                .AllowAnonymous()
                .ExcludeFromDescription();
            app.MapGet(FormerBasePath, FormerPage).AllowAnonymous().ExcludeFromDescription();
            app.MapGet($"{FormerBasePath}/{{**path}}", FormerPage).AllowAnonymous().ExcludeFromDescription();

            if (app.SpaFiles() is { } files)
            {
                app.MapFallbackToFile("{*path:nonfile}", "index.html", new StaticFileOptions
                {
                    FileProvider = files,
                    OnPrepareResponse = static context => context.Context.Response.Headers.CacheControl = "no-cache",
                })
                .AllowAnonymous();
            }

            return app;
        }

        /// <summary>
        /// Opens the folder of the built application.
        /// </summary>
        /// <returns>The file provider, or <see langword="null"/> when the client is not built.</returns>
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
    /// Sets the cache headers of a client file: hashed bundles never change, the rest is revalidated.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="path">The request path.</param>
    private static void SetCacheHeaders(HttpResponse response, PathString path)
        => response.Headers.CacheControl = path.StartsWithSegments("/assets", StringComparison.OrdinalIgnoreCase)
            ? "public, max-age=31536000, immutable"
            : CacheControlHeaderValue.NoCacheString;

    /// <summary>
    /// Redirects a former <c>/app/…</c> address to the same page at the root, with its query string.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The redirect.</returns>
    private static IResult FormerPage(HttpContext context)
    {
        var path = context.Request.Path.Value![FormerBasePath.Length..];
        return Results.LocalRedirect($"{(path.Length == 0 ? "/" : path)}{context.Request.QueryString}");
    }

    /// <summary>
    /// Logs that the client application is missing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="root">The expected folder.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The client application is not built ({Root}); run `pnpm dev` or `pnpm build` in src/KOD.Client")]
    private static partial void LogSpaMissing(ILogger logger, string root);
}
