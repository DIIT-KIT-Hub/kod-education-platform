using KOD.Domain.Localization;
using KOD.Infrastructure.Persistence.Seeding;
using KOD.Web.Media;
using KOD.Web.Realtime;
using KOD.Web.Security;
using KOD.Web.Signing;
using Scalar.AspNetCore;

namespace KOD.Web.Hosting;

/// <summary>
/// Configures the HTTP pipeline and endpoints.
/// </summary>
internal static class WebApplicationExtensions
{
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        /// <summary>
        /// Applies database migrations and seeds the initial data.
        /// </summary>
        /// <returns>A task that completes when the database is ready.</returns>
        public async Task InitializeDatabaseAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(app.Lifetime.ApplicationStopping);
        }

        /// <summary>
        /// Configures middleware and maps endpoints.
        /// </summary>
        /// <returns>The web application.</returns>
        public WebApplication UseWebPipeline()
        {
            // First: the client address and scheme from trusted reverse proxies (rate limits, audit, session IPs, HTTPS).
            app.UseForwardedHeaders();
            app.UseRequestLogging();

            // Failures answer with problem details (RFC 9457); the pages are the static client.
            app.UseExceptionHandler();
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseHttpsRedirection();
            app.UseSpaFiles();

            app.UseRouting();
            app.UseAuthentication();

            // After authentication: the language saved in the user's profile is a claim.
            app.UseRequestLocalization();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.UseMiddleware<AccountSetupMiddleware>();

            app.MapStaticAssets().AllowAnonymous();
            app.MapControllers();
            app.MapHub<LiveHub>(LiveHub.Route);
            app.MapEuSignProxy();
            app.MapTrustList();
            app.MapMediaEndpoints();
            app.MapSpaFallback();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi().AllowAnonymous();
                app.MapScalarApiReference(static options => options.WithTitle(Texts.T("КОД REST API"))).AllowAnonymous();
            }

            return app;
        }
    }
}
