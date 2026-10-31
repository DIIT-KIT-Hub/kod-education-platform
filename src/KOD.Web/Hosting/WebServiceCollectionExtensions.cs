using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KOD.Application.Abstractions;
using KOD.Application.Features.News.Discussion;
using KOD.Domain.Localization;
using KOD.Infrastructure.Identity.Tokens;
using KOD.Web.Identity;
using KOD.Web.Localization;
using KOD.Web.Realtime;
using KOD.Web.Security;
using KOD.Web.Signing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Hosting;

/// <summary>
/// Registers the presentation layer: Blazor, REST API, JWT authentication, authorization and security services.
/// </summary>
internal static class WebServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the presentation layer services.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddWebServices(IConfiguration configuration)
        {
            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddControllers(static options => options.Filters.Add<KOD.Web.Api.ApiAntiforgeryFilter>())
                .AddJsonOptions(static options => ConfigureJson(options.JsonSerializerOptions));

            // The OpenAPI document describes types with these options: keep them equal to the controllers' ones, so the
            // generated client types match the real payloads (enums as names, numbers as numbers).
            services.ConfigureHttpJsonOptions(static options => ConfigureJson(options.SerializerOptions));
            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddOpenApi();
            services.AddSignalR();
            services.AddSingleton<INewsDiscussionEvents, LiveNewsDiscussionEvents>();

            services.AddJwtAuthentication();
            services.AddPermissionAuthorization();
            services.AddRateLimiting();
            services.AddTrustedProxies(configuration);

            services.AddAntiforgery(static options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "__Host-kod.af";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            services.AddScoped<CircuitUserAccessor>();
            services.AddScoped<CurrentUser>();
            services.AddScoped<ICurrentUser>(static sp => sp.GetRequiredService<CurrentUser>());
            services.AddScoped<CircuitHandler, UserCircuitHandler>();
            services.AddScoped<IdentityRedirectManager>();

            services.Configure<SpaOptions>(configuration.GetSection(SpaOptions.SectionName));
            services.Configure<SigningOptions>(configuration.GetSection(SigningOptions.SectionName));
            services.AddHttpClient(EuSignProxyEndpoint.HttpClientName, static (sp, client) =>
                {
                    var seconds = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SigningOptions>>().Value.ProxyTimeoutSeconds;
                    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 60));
                })
                .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    UseCookies = false,
                });

            services.AddSingleton<TrustListStore>();
            services.AddHostedService<TrustListRefreshService>();
            services.AddHttpClient(TrustListRefreshService.HttpClientName, static client => client.Timeout = TimeSpan.FromMinutes(2))
                .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });

            services.AddRequestLocalization(static options =>
            {
                var cultures = Languages.All.Select(Languages.CultureOf).ToArray();
                options.SetDefaultCulture(Languages.CultureOf(Languages.Ukrainian));
                options.AddSupportedCultures(cultures);
                options.AddSupportedUICultures(cultures);
                options.RequestCultureProviders = [new LanguageCultureProvider()];
            });

            return services;
        }

        /// <summary>
        /// Configures stateless JWT authentication: <see cref="JwtAuthenticationHandler"/> for signed-in users and
        /// <see cref="TwoFactorJwtHandler"/> for the step between the password and the second factor.
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddJwtAuthentication()
        {
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            services.AddAuthentication(JwtDefaults.AuthenticationScheme)
                .AddScheme<JwtAuthenticationOptions, JwtAuthenticationHandler>(JwtDefaults.AuthenticationScheme, static _ => { })
                .AddScheme<AuthenticationSchemeOptions, TwoFactorJwtHandler>(IdentityConstants.TwoFactorUserIdScheme, static _ => { });

            return services;
        }

        /// <summary>
        /// Accepts <c>X-Forwarded-For</c> / <c>X-Forwarded-Proto</c> only from trusted reverse proxies: loopback and the
        /// addresses in <c>ReverseProxy:KnownProxies</c> / <c>ReverseProxy:KnownNetworks</c> (CIDR). Headers from anyone
        /// else are ignored, so clients cannot spoof their IP to evade rate limits or falsify the audit log.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddTrustedProxies(IConfiguration configuration)
        {
            var proxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
            var networks = configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [];
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                foreach (var proxy in proxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }

                foreach (var network in networks)
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            });

            return services;
        }

        /// <summary>
        /// Adds permission-based authorization; every endpoint requires an authenticated user unless marked anonymous.
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddPermissionAuthorization()
        {
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            return services;
        }

        /// <summary>
        /// Adds rate limits: account forms (brute force), the REST API and the signing relay.
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddRateLimiting()
        {
            services.AddRateLimiter(static options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(static context =>
                {
                    var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Forms that verify passwords or codes: sign-in pages, the authentication API and the profile security actions.
                    if (HttpMethods.IsPost(context.Request.Method)
                        && (context.Request.Path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase)
                            || context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
                            || context.Request.Path.StartsWithSegments("/profile", StringComparison.OrdinalIgnoreCase)))
                    {
                        return RateLimitPartition.GetFixedWindowLimiter($"account:{client}", static _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                        });
                    }

                    if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = context.User.UserId?.ToString() ?? client;
                        return RateLimitPartition.GetTokenBucketLimiter($"api:{key}", static _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 300,
                            TokensPerPeriod = 300,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        });
                    }

                    return RateLimitPartition.GetNoLimiter("none");
                });

                options.AddPolicy(EuSignProxyEndpoint.RateLimitPolicy, static context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        $"eusign:{context.User.UserId?.ToString() ?? context.Connection.RemoteIpAddress?.ToString()}",
                        static _ => new FixedWindowRateLimiterOptions { PermitLimit = 240, Window = TimeSpan.FromMinutes(1) }));
            });

            return services;
        }
    }

    /// <summary>
    /// Applies the API JSON conventions: enums as their names, numbers only as JSON numbers.
    /// </summary>
    /// <param name="options">The serializer options.</param>
    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.NumberHandling = JsonNumberHandling.Strict;
    }
}
