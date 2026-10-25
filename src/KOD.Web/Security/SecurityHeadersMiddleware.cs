namespace KOD.Web.Security;

/// <summary>
/// Adds security headers to every response, including a strict Content-Security-Policy.
/// No inline scripts and no <c>eval</c> are allowed: the official signing library is asm.js without dynamic code execution.
/// </summary>
/// <param name="next">The next middleware.</param>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is produced.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(ApplyHeaders, context);
        return next(context);
    }

    /// <summary>
    /// Writes the headers right before the response starts.
    /// </summary>
    /// <param name="state">The HTTP context.</param>
    /// <returns>A completed task.</returns>
    private static Task ApplyHeaders(object state)
    {
        var http = (HttpContext)state;
        var headers = http.Response.Headers;
        var socket = http.Request.IsHttps ? "wss" : "ws";
        var host = http.Request.Host.Value;

        headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; " +
            $"connect-src 'self' {socket}://{host}; worker-src 'self'; manifest-src 'self'; object-src 'none'; base-uri 'self'; " +
            "form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests";
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        return Task.CompletedTask;
    }
}
