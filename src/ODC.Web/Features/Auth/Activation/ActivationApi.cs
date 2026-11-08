using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The HTTP side shared by the activation slices: the route group (anonymous, the activation rate limit on code
/// requests) and the encrypted <c>__Host-</c> cookie that binds an activation to one browser (scripts cannot read it).
/// The rules themselves are in <see cref="AccountActivationService"/>.
/// </summary>
internal static class ActivationApi
{
    /// <summary>
    /// The name of the rate limit policy of code requests.
    /// </summary>
    public const string RateLimitPolicy = "activation";

    /// <summary>
    /// The cookie that binds an activation to this browser.
    /// </summary>
    public const string CookieName = "__Host-odc.activation";

    /// <summary>
    /// The error code of a browser without an activation.
    /// </summary>
    private const string NoActivationCode = "no_activation";

    /// <summary>
    /// The route group <c>/api/v1/auth/activation</c> (before signing in).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapActivation(IEndpointRouteBuilder api) => api.MapGroup("auth/activation").AllowAnonymous().WithTags("AccountActivation");

    /// <summary>
    /// The error of a browser without an activation.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NoActivation()
        => new(NoActivationCode, Texts.T("Активацію не розпочато або час на неї минув. Почніть знову."), ErrorType.NotFound);

    /// <summary>
    /// Converts a state to the response.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The response.</returns>
    public static ActivationStatusResponse ToResponse(ActivationStatus status) => new(status.Stage, status.Email, status.ResendAfterSeconds);

    /// <summary>
    /// Reads the activation of this browser.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="activation">The activation engine.</param>
    /// <returns>The activation, or <see langword="null"/>.</returns>
    public static ActivationFlow? CurrentFlow(HttpRequest request, AccountActivationService activation) => activation.ReadTicket(request.Cookies[CookieName]);

    /// <summary>
    /// Binds the activation to this browser.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="ticket">The protected ticket.</param>
    public static void WriteCookie(HttpResponse response, string ticket)
        => response.Cookies.Append(CookieName, ticket, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = AccountActivationService.TicketLifetime,
        });

    /// <summary>
    /// Removes the activation cookie.
    /// </summary>
    /// <param name="response">The response.</param>
    public static void DeleteCookie(HttpResponse response)
        => response.Cookies.Delete(CookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" });
}
