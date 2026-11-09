using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ODC.Web.Hosting;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Security;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Web.Security;

/// <summary>
/// The security middleware and policies: response headers (CSP), the mandatory profile photo, how exceptions become
/// problem details, permission policies and the current user.
/// </summary>
public sealed class SecurityTests
{
    [Fact]
    public async Task Every_response_carries_a_strict_content_security_policy_and_the_hardening_headers()
    {
        var context = Context("/documents", https: true, host: "odc.example.com");

        await new SecurityHeadersMiddleware(static _ => Task.CompletedTask).InvokeAsync(context);
        await StartResponse(context);

        var headers = context.Response.Headers;
        var csp = headers.ContentSecurityPolicy.ToString();
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("script-src 'self';", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.Contains("connect-src 'self' wss://odc.example.com", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("object-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("base-uri 'self'", csp, StringComparison.Ordinal);
        Assert.Equal("nosniff", headers.XContentTypeOptions.ToString());
        Assert.Equal("DENY", headers.XFrameOptions.ToString());
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
        Assert.Contains("camera=()", headers["Permissions-Policy"].ToString(), StringComparison.Ordinal);
        Assert.Equal("same-origin", headers["Cross-Origin-Opener-Policy"].ToString());
        Assert.Equal("same-origin", headers["Cross-Origin-Resource-Policy"].ToString());
    }

    [Fact]
    public async Task A_plain_http_page_allows_the_plain_web_socket_of_its_own_host()
    {
        var context = Context("/", https: false, host: "localhost:5000");

        await new SecurityHeadersMiddleware(static _ => Task.CompletedTask).InvokeAsync(context);
        await StartResponse(context);

        Assert.Contains("connect-src 'self' ws://localhost:5000", context.Response.Headers.ContentSecurityPolicy.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v1/documents", false)]
    [InlineData("/API/v1/news", false)]
    [InlineData("/api/v1/auth/context", true)]
    [InlineData("/api/v1/profile/photo", true)]
    [InlineData("/documents", true)]
    [InlineData("/media/abc", true)]
    public async Task A_user_without_a_photo_reaches_only_the_sign_in_the_photo_and_the_pages(string path, bool allowed)
    {
        var context = Context(path, user: User(photo: null));
        var reached = false;

        await new AccountSetupMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.Equal(allowed, reached);
        if (!allowed)
        {
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.Contains("фото профілю", await Body(context), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task A_user_with_a_photo_and_a_visitor_pass_through()
    {
        var withPhoto = Context("/api/v1/documents", user: User(photo: Guid.NewGuid()));
        var visitor = Context("/api/v1/documents");
        var passed = 0;
        var middleware = new AccountSetupMiddleware(_ =>
        {
            passed++;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(withPhoto);
        await middleware.InvokeAsync(visitor);

        Assert.Equal(2, passed);
    }

    [Theory]
    [InlineData("domain", 400)]
    [InlineData("bad-request", 413)]
    [InlineData("forbidden", 403)]
    [InlineData("other", 500)]
    public async Task Exceptions_of_the_api_become_problem_details_without_internals(string kind, int status)
    {
        Exception exception = kind switch
        {
            "domain" => new DomainException("Назва обов'язкова."),
            "bad-request" => new BadHttpRequestException("too large", StatusCodes.Status413PayloadTooLarge),
            "forbidden" => new UnauthorizedAccessException("needs roles.manage"),
            _ => new InvalidOperationException("Connection string password=secret"),
        };
        var context = Context("/api/v1/roles", services: ProblemServices());

        var handled = await new ApiExceptionHandler(context.RequestServices.GetRequiredService<IProblemDetailsService>(), NullLogger<ApiExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(status, context.Response.StatusCode);
        var body = await Body(context);
        Assert.DoesNotContain("secret", body, StringComparison.Ordinal);
        Assert.DoesNotContain("roles.manage", body, StringComparison.Ordinal);
        if (kind == "domain")
        {
            Assert.Contains("Назва обов'язкова.", JsonDocument.Parse(body).RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Exceptions_outside_the_api_are_left_to_the_page_handler()
    {
        var context = Context("/documents", services: ProblemServices());

        var handled = await new ApiExceptionHandler(context.RequestServices.GetRequiredService<IProblemDetailsService>(), NullLogger<ApiExceptionHandler>.Instance)
            .TryHandleAsync(context, new InvalidOperationException(), TestContext.Current.CancellationToken);

        Assert.False(handled);
    }

    [Fact]
    public async Task A_permission_policy_needs_a_signed_in_user_with_any_of_its_permissions()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var attribute = new RequirePermissionAttribute("users.manage", "users.view");
        var policy = await provider.GetPolicyAsync(attribute.Policy!);

        Assert.NotNull(policy);
        Assert.Equal(["users.manage", "users.view"], attribute.Permissions);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
        Assert.Equal(["users.manage", "users.view"], requirement.Permissions);
        Assert.Contains(policy.Requirements, static r => r is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);

        Assert.True(await Allowed(requirement, User(photo: null, "users.view")));
        Assert.False(await Allowed(requirement, User(photo: null, "news.view")));
        Assert.False(await Allowed(requirement, new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public async Task Other_policies_are_left_to_the_default_provider()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("named", static p => p.RequireRole("x"));
        var provider = new PermissionPolicyProvider(Options.Create(options));

        Assert.NotNull(await provider.GetPolicyAsync("named"));
        Assert.Null(await provider.GetPolicyAsync("unknown"));
    }

    [Fact]
    public void The_current_user_is_read_from_the_token_claims()
    {
        var id = Guid.NewGuid();
        var photo = Guid.NewGuid();
        var principal = User(photo, "documents.view", "news.view");
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(AppClaimTypes.Subject, id.ToString()));
        var context = Context("/api/v1/profile", user: principal);
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.1.2.3");
        var user = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.True(user.IsAuthenticated);
        Assert.Equal(id, user.Id);
        Assert.Equal("Шевченко Тарас", user.DisplayName);
        Assert.Equal("10.1.2.3", user.IpAddress);
        Assert.True(user.Has("news.view"));
        Assert.False(user.Has("users.manage"));
        Assert.Equal(photo, principal.PhotoId);
        Assert.True(principal.HasAnyPermission("users.manage", "news.view"));
        Assert.False(principal.HasAnyPermission("users.manage"));
        Assert.Equal("Тарас", principal.ShortName);
    }

    [Fact]
    public void A_visitor_has_no_identity_and_no_permissions()
    {
        var user = new CurrentUser(new HttpContextAccessor { HttpContext = Context("/") });

        Assert.False(user.IsAuthenticated);
        Assert.Null(user.Id);
        Assert.Null(user.DisplayName);
        Assert.Empty(user.Permissions);
        Assert.Null(new CurrentUser(new HttpContextAccessor()).IpAddress);
    }

    /// <summary>
    /// Builds a request.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="https">Whether the request is HTTPS.</param>
    /// <param name="host">The host.</param>
    /// <param name="user">The signed-in user.</param>
    /// <param name="services">The request services.</param>
    /// <returns>The context with a readable body.</returns>
    private static DefaultHttpContext Context(string path, bool https = true, string host = "localhost", ClaimsPrincipal? user = null, IServiceProvider? services = null)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartableResponse());
        context.RequestServices = services ?? new ServiceCollection().BuildServiceProvider();
        context.Request.Path = path;
        context.Request.Scheme = https ? "https" : "http";
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();
        if (user is not null)
        {
            context.User = user;
        }

        return context;
    }

    /// <summary>
    /// A signed-in user with a full name, an optional photo and permissions.
    /// </summary>
    /// <param name="photo">The photo.</param>
    /// <param name="permissions">The permissions.</param>
    /// <returns>The principal.</returns>
    private static ClaimsPrincipal User(Guid? photo, params string[] permissions)
    {
        var identity = new ClaimsIdentity("Jwt");
        identity.AddClaim(new Claim(AppClaimTypes.FullName, "Шевченко Тарас"));
        identity.AddClaim(new Claim(AppClaimTypes.ShortName, "Тарас"));
        if (photo is { } id)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Photo, id.ToString("N")));
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// The services that write problem details.
    /// </summary>
    /// <returns>The services.</returns>
    private static ServiceProvider ProblemServices() => new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

    /// <summary>
    /// Runs the callbacks registered for the start of the response (DefaultHttpContext never starts one itself).
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>A task that completes when the callbacks ran.</returns>
    private static Task StartResponse(DefaultHttpContext context)
        => context.Features.Get<IHttpResponseFeature>() is StartableResponse startable
            ? startable.StartAsync()
            : Task.CompletedTask;

    /// <summary>
    /// Reads the response body.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The body.</returns>
    private static async Task<string> Body(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Tells whether a requirement is met by a user.
    /// </summary>
    /// <param name="requirement">The requirement.</param>
    /// <param name="user">The user.</param>
    /// <returns>Whether it succeeded.</returns>
    private static async Task<bool> Allowed(PermissionRequirement requirement, ClaimsPrincipal user)
    {
        var context = new AuthorizationHandlerContext([requirement], user, null);
        await new PermissionAuthorizationHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    /// <summary>
    /// A response feature that runs the callbacks registered for the start of the response when the test says so.
    /// </summary>
    private sealed class StartableResponse : HttpResponseFeature
    {
        /// <summary>
        /// The callbacks.
        /// </summary>
        private readonly List<(Func<object, Task> Callback, object State)> _starting = [];

        /// <inheritdoc />
        public override void OnStarting(Func<object, Task> callback, object state) => _starting.Add((callback, state));

        /// <summary>
        /// Runs the callbacks.
        /// </summary>
        /// <returns>A task that completes when they ran.</returns>
        public async Task StartAsync()
        {
            foreach (var (callback, state) in _starting)
            {
                await callback(state);
            }
        }
    }
}
