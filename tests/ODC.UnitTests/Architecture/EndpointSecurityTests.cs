using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ODC.Web.Endpoints;
using ODC.Web.Features;
using ODC.Web.Features.Media;
using ODC.Web.Features.Signing;
using ODC.Web.Hosting;
using ODC.Web.Infrastructure;

namespace ODC.UnitTests.Architecture;

/// <summary>
/// Every endpoint of the real host (the same service registration and mapping as <c>Program</c>, no database
/// connection): deny by default, anonymous only where meant, and the antiforgery token for every state-changing request
/// (OWASP API2 / API5, CSRF).
/// </summary>
public sealed class EndpointSecurityTests : IAsyncDisposable
{
    /// <summary>
    /// The endpoints open to anonymous visitors: the sign-in and the account activation.
    /// </summary>
    private static readonly HashSet<string> Anonymous =
    [
        "GET /api/v1/auth/context",
        "POST /api/v1/auth/login",
        "GET /api/v1/auth/login/pending",
        "POST /api/v1/auth/login/two-factor",
        "POST /api/v1/auth/login/recovery",
        "GET /api/v1/auth/login/authenticator",
        "POST /api/v1/auth/login/authenticator",
        "POST /api/v1/auth/logout",
        "PUT /api/v1/auth/language",
        "GET /api/v1/auth/activation",
        "POST /api/v1/auth/activation",
        "DELETE /api/v1/auth/activation",
        "POST /api/v1/auth/activation/code",
        "POST /api/v1/auth/activation/password",
    ];

    /// <summary>
    /// State-changing endpoints without the antiforgery token: the relay of public CA messages (OCSP, TSP, CMP) sent by
    /// the signing library, which changes nothing in the cabinet.
    /// </summary>
    private static readonly HashSet<string> WithoutAntiforgery = ["POST /api/v1/eusign/proxy"];

    /// <summary>
    /// The host.
    /// </summary>
    private readonly WebApplication _app;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointSecurityTests"/> class.
    /// </summary>
    public EndpointSecurityTests()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(IEndpoint).Assembly.GetName().Name,
            EnvironmentName = Environments.Development,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=odc_never_opened",
        });
        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration, builder.Environment)
            .AddWebServices(builder.Configuration);

        _app = builder.Build();
        _app.MapApiEndpoints(typeof(IEndpoint).Assembly);
        _app.MapEuSignProxy();
        _app.MapTrustList();
        _app.MapMediaEndpoints();
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public void The_api_has_endpoints_to_check()
        => Assert.True(Endpoints().Count() > 100);

    [Fact]
    public void Only_the_sign_in_and_the_activation_are_anonymous()
    {
        var anonymous = Endpoints().Where(static e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null).Select(static e => e.Name).ToHashSet();

        Assert.Empty(anonymous.Except(Anonymous));
        Assert.Empty(Anonymous.Except(anonymous));
    }

    [Fact]
    public void Every_other_endpoint_requires_a_signed_in_user()
    {
        var open = Endpoints()
            .Where(static e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null && e.Endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0)
            .Select(static e => e.Name);

        // The media group relies on the fallback policy (signed-in users), which applies to endpoints without any
        // authorization metadata; everything else states it.
        Assert.All(open, static name => Assert.StartsWith("GET /media/", name, StringComparison.Ordinal));
    }

    [Fact]
    public void Every_state_changing_endpoint_needs_the_antiforgery_token()
    {
        var unprotected = Endpoints()
            .Where(static e => !e.Safe && e.Endpoint.Metadata.GetMetadata<AntiforgeryTokenRequired>() is null)
            .Select(static e => e.Name)
            .ToHashSet();

        Assert.Equal(WithoutAntiforgery.Order(), unprotected.Order());
    }

    /// <summary>
    /// Lists the endpoints, one entry per HTTP method.
    /// </summary>
    /// <returns>The endpoints with their names ("METHOD /route") and whether the method is safe.</returns>
    private IEnumerable<(string Name, bool Safe, RouteEndpoint Endpoint)> Endpoints()
        => from endpoint in ((IEndpointRouteBuilder)_app).DataSources.SelectMany(static s => s.Endpoints).OfType<RouteEndpoint>()
           from method in endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []
           let route = "/" + endpoint.RoutePattern.RawText!.Trim('/')
           select ($"{method} {route}", HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method), endpoint);
}
