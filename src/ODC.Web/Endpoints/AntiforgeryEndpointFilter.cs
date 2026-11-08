using Microsoft.AspNetCore.Antiforgery;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Endpoints;

/// <summary>
/// Requires a valid antiforgery token (header <c>X-CSRF-TOKEN</c>) on every state-changing API request: POST, PUT,
/// PATCH and DELETE. The token comes with the authentication context and is bound to the signed-in identity. Applied to
/// the whole <c>/api/v1</c> group, so an endpoint cannot forget it; the framework's own form-only check is disabled in
/// favour of this one.
/// </summary>
/// <param name="antiforgery">The antiforgery service.</param>
internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        {
            return await next(context);
        }

        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            return TypedResults.Problem(
                detail: Texts.T("Сесію сторінки оновлено. Оновіть сторінку та повторіть дію."),
                statusCode: StatusCodes.Status400BadRequest,
                title: "antiforgery");
        }

        return await next(context);
    }
}
