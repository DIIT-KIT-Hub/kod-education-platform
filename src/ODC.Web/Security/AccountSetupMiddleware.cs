using ODC.Web.Shared.Localization;

namespace ODC.Web.Security;

/// <summary>
/// Makes signed-in users finish their account before using the cabinet: upload a profile photo. The REST API answers 403
/// until then; the client application leads the user to the same step (its pages, images and scripts stay available).
/// </summary>
/// <param name="next">The next middleware.</param>
internal sealed class AccountSetupMiddleware(RequestDelegate next)
{
    /// <summary>
    /// API paths that stay available during the setup: authentication (context, language, sign-out) and the photo upload.
    /// </summary>
    private static readonly PathString[] AllowedPaths = [new("/api/v1/auth"), new("/api/v1/profile/photo")];

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is produced.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true
            || !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || Array.Exists(AllowedPaths, p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            return next(context);
        }

        return user.PhotoId is null ? Refuse(context, Texts.T("Додайте фото профілю, щоб користуватися кабінетом.")) : next(context);
    }

    /// <summary>
    /// Answers an API request with 403 and the step to finish.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="message">The step to finish.</param>
    /// <returns>A task that completes when the response is written.</returns>
    private static Task Refuse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { title = message });
    }
}
