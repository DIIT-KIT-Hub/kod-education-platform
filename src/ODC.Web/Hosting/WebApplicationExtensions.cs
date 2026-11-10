using ODC.Web.Endpoints;
using ODC.Web.Features.Media;
using ODC.Web.Features.Signing;
using ODC.Web.Infrastructure.Persistence.Seeding;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Realtime;
using ODC.Web.Security;
using ODC.Web.Shared.Localization;
using Scalar.AspNetCore;

namespace ODC.Web.Hosting;

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

            // An organization's addresses (/o/{code}/…) are served as the plain ones with the organization remembered.
            app.UseMiddleware<OrganizationPathMiddleware>();
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

            // After authentication, before authorization: the organization of the request and the permissions there.
            app.UseMiddleware<OrganizationMiddleware>();

            // After authentication: the language saved in the user's profile is a claim.
            app.UseRequestLocalization();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.UseMiddleware<AccountSetupMiddleware>();

            app.MapStaticAssets().AllowAnonymous();
            app.MapApiEndpoints(typeof(IEndpoint).Assembly);

            // SignalR checks the user only when connecting: closing at the token expiry makes the client reconnect with
            // a fresh token, so a blocked user or an ended sign-in stops receiving chat messages.
            app.MapHub<LiveHub>(LiveHub.Route, static options => options.CloseOnAuthenticationExpiration = true);

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
