using KOD.Domain.Localization;

namespace KOD.Web.Security;

/// <summary>
/// Makes signed-in users finish their account before using the cabinet, one step at a time: first replace a temporary
/// password, then upload a profile photo. Pages redirect to the step; the REST API answers 403.
/// </summary>
/// <param name="next">The next middleware.</param>
internal sealed class AccountSetupMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The page where the password is changed.
    /// </summary>
    public const string ChangePasswordPath = "/account/change-password";

    /// <summary>
    /// The page where the profile photo is uploaded.
    /// </summary>
    public const string PhotoPath = "/account/photo";

    /// <summary>
    /// Paths that stay available during the setup: the steps themselves, signing out, the language switch, assets
    /// and images (the photo preview), error pages, and the single-page application with its authentication API (it
    /// guides the user through the same steps itself).
    /// </summary>
    private static readonly PathString[] AllowedPaths =
    [
        new(ChangePasswordPath), new(PhotoPath), new("/api/v1/profile/photo"), new("/account/logout"), new("/account/language"), new("/_framework"), new("/_blazor"),
        new("/app"), new("/api/v1/auth"),
        new("/css"), new("/js"), new("/fonts"), new("/icons"), new("/img"), new("/lib"), new("/media"), new("/favicon.svg"),
        new("/error"), new("/not-found"),
    ];

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
            || Array.Exists(AllowedPaths, p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            return next(context);
        }

        if (user.MustChangePassword)
        {
            return Divert(context, ChangePasswordPath, Texts.T("Потрібно змінити тимчасовий пароль."));
        }

        return user.PhotoId is null ? Divert(context, PhotoPath, Texts.T("Додайте фото профілю, щоб користуватися кабінетом.")) : next(context);
    }

    /// <summary>
    /// Sends the request to a setup step.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="step">The page of the step.</param>
    /// <param name="message">The reason for API clients.</param>
    /// <returns>A task that completes when the response is produced.</returns>
    private static Task Divert(HttpContext context, string step, string message)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new { title = message });
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{step}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    }
}
