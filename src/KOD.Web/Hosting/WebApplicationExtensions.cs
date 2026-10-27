using KOD.Domain.Localization;
using KOD.Infrastructure.Persistence.Seeding;
using KOD.Web.Components;
using KOD.Web.Identity;
using KOD.Web.Localization;
using KOD.Web.Media;
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

            if (app.Environment.IsDevelopment())
            {
                // HTML pages keep the developer exception page; the API still answers with problem details.
                app.UseWhen(
                    static context => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
                    static branch => branch.UseExceptionHandler());
            }
            else
            {
                app.UseExceptionHandler("/error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseWhen(
                static context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
                static branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();

            // After authentication: the language saved in the user's profile is a claim.
            app.UseRequestLocalization();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.UseMiddleware<AccountSetupMiddleware>();
            app.UseAntiforgery();

            app.MapStaticAssets().AllowAnonymous();
            app.MapControllers();
            app.MapEuSignProxy();
            app.MapTrustList();
            app.MapAccountEndpoints();
            app.MapMediaEndpoints();
            app.MapLanguageEndpoint();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi().AllowAnonymous();
                app.MapScalarApiReference(static options => options.WithTitle(Texts.T("КОД REST API"))).AllowAnonymous();
            }

            return app;
        }
    }
}
