using KOD.Domain.Common;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Hosting;

/// <summary>
/// Converts unhandled exceptions of <c>/api</c> requests into RFC 9457 problem details without leaking internals.
/// Other requests fall through to the HTML error page.
/// </summary>
/// <param name="problemDetails">The problem details service.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var (status, detail) = exception switch
        {
            DomainException domain => (StatusCodes.Status400BadRequest, domain.Message),
            BadHttpRequestException bad => (bad.StatusCode, Texts.T("Некоректний запит.")),
            _ => (StatusCodes.Status500InternalServerError, Texts.T("Сталася внутрішня помилка. Спробуйте пізніше.")),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, httpContext.Request.Path, exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail },
        });
    }

    /// <summary>
    /// Logs an unhandled exception.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The request path.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Path}")]
    private static partial void LogUnhandled(ILogger logger, string path, Exception exception);
}
