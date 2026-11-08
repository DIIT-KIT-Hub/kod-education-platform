namespace ODC.Web.Features.Auth;

/// <summary>
/// The route group of the authentication endpoints. Tokens stay in <c>HttpOnly</c> cookies set by the JWT handler; the
/// responses carry no secrets except the authenticator key and the recovery codes, shown once to their owner. POSTs to
/// <c>/api/v1/auth</c> share the per-client sign-in rate limit (see the rate limiter).
/// </summary>
internal static class AuthApi
{
    /// <summary>
    /// The route group <c>/api/v1/auth</c>.
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapAuth(IEndpointRouteBuilder api) => api.MapGroup("auth").WithTags("Auth");
}
