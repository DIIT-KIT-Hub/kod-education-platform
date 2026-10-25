namespace KOD.Web.Security;

/// <summary>
/// Forces users with a temporary password to set their own one before using the cabinet.
/// </summary>
/// <param name="next">The next middleware.</param>
internal sealed class PasswordChangeEnforcementMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The page where the password is changed.
    /// </summary>
    public const string ChangePasswordPath = "/account/change-password";

    /// <summary>
    /// Paths that stay available with a temporary password.
    /// </summary>
    private static readonly PathString[] AllowedPaths =
    [
        new(ChangePasswordPath), new("/account/logout"), new("/_framework"), new("/_content"), new("/_blazor"),
        new("/css"), new("/js"), new("/img"), new("/lib"), new("/favicon.svg"), new("/error"), new("/not-found"),
    ];

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is produced.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated != true
            || !context.User.MustChangePassword
            || Array.Exists(AllowedPaths, p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            return next(context);
        }

        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new { title = "Потрібно змінити тимчасовий пароль." });
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{ChangePasswordPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    }
}
