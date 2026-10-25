using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KOD.Web.Api;

/// <summary>
/// Protects state-changing REST requests against CSRF: the browser must send the antiforgery token
/// (header <c>X-CSRF-TOKEN</c>, rendered into the <c>csrf-token</c> meta tag of every page) together with the cookie.
/// Safe methods (GET, HEAD, OPTIONS, TRACE) are not checked.
/// </summary>
/// <param name="antiforgery">The antiforgery service.</param>
internal sealed class ApiAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        {
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "antiforgery",
                Detail = "Сесію сторінки оновлено. Оновіть сторінку та повторіть дію.",
            })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }
    }
}
