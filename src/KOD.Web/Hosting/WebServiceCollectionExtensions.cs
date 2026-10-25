using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KOD.Application.Abstractions;
using KOD.Infrastructure.Identity;
using KOD.Web.Identity;
using KOD.Web.Security;
using KOD.Web.Signing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KOD.Web.Hosting;

/// <summary>
/// Registers the presentation layer: Blazor, REST API, authentication cookies, authorization and security services.
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
                .AddJsonOptions(static options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddOpenApi();

            services.AddCookieAuthentication();
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
            services.AddScoped<ISessionContext>(static sp => sp.GetRequiredService<CurrentUser>());
            services.AddScoped<CircuitHandler, UserCircuitHandler>();
            services.AddScoped<IdentityRedirectManager>();

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
                options.SetDefaultCulture("uk-UA");
                options.AddSupportedCultures("uk-UA");
                options.AddSupportedUICultures("uk-UA");
            });

            return services;
        }

        /// <summary>
        /// Configures the ASP.NET Core Identity cookies (session authentication).
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddCookieAuthentication()
        {
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            services.AddAuthentication(static options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddIdentityCookies(static cookies =>
                {
                    cookies.TwoFactorUserIdCookie?.Configure(static options =>
                    {
                        options.Cookie.Name = "__Host-kod.2fa";
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                        options.Cookie.SameSite = SameSiteMode.Strict;
                        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    });
                });

            services.ConfigureApplicationCookie(static options =>
            {
                options.Cookie.Name = "__Host-kod";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.ReturnUrlParameter = "returnUrl";

                // API clients get status codes instead of HTML redirects.
                options.Events.OnRedirectToLogin = static context => RedirectOrStatus(context.HttpContext, context.RedirectUri, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = static context => RedirectOrStatus(context.HttpContext, context.RedirectUri, StatusCodes.Status403Forbidden);
            });

            // Idle timeout (sliding); the absolute lifetime is enforced by the server-side ticket store.
            services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
                .Configure<IOptions<SessionLifetimeOptions>>(static (options, lifetime) => options.ExpireTimeSpan = lifetime.Value.IdleTimeout);

            // Permissions are re-read from the database every minute (claims factory). The rebuilt principal keeps the claims
            // that describe the sign-in itself rather than the user: the server-side session and the authentication methods.
            services.Configure<SecurityStampValidatorOptions>(static options =>
            {
                options.ValidationInterval = TimeSpan.FromMinutes(1);
                options.OnRefreshingPrincipal = static context =>
                {
                    if (context.NewPrincipal?.Identity is ClaimsIdentity identity && context.CurrentPrincipal is { } current)
                    {
                        foreach (var type in (string[])[AppClaimTypes.SessionId, AppClaimTypes.AuthenticationMethod])
                        {
                            if (identity.FindFirst(type) is null)
                            {
                                identity.AddClaims(current.FindAll(type).Select(static c => new Claim(c.Type, c.Value)));
                            }
                        }
                    }

                    return Task.CompletedTask;
                };
            });

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

                    // Forms that verify passwords or codes: sign-in pages and the profile security actions.
                    if (HttpMethods.IsPost(context.Request.Method)
                        && (context.Request.Path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase)
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
    /// Redirects browser requests and returns a status code for API requests.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="redirectUri">The redirect target.</param>
    /// <param name="status">The status code for API requests.</param>
    /// <returns>A completed task.</returns>
    private static Task RedirectOrStatus(HttpContext context, string redirectUri, int status)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = status;
        }
        else
        {
            context.Response.Redirect(redirectUri);
        }

        return Task.CompletedTask;
    }
}
