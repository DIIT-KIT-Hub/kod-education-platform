using Microsoft.AspNetCore.Components;

namespace KOD.Web.Identity;

/// <summary>
/// Redirect helpers for the account pages. Only local URLs are followed, which prevents open redirects.
/// </summary>
/// <param name="navigationManager">The navigation manager.</param>
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    /// <summary>
    /// Normalizes a return URL to a site-local path.
    /// </summary>
    /// <param name="uri">The candidate URL.</param>
    /// <returns>A local URL.</returns>
    public static string SafeLocal(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return "/";
        }

        // Only "/path" style URLs are accepted; "//host" and "/\host" are protocol-relative and rejected.
        return uri.StartsWith('/') && !uri.StartsWith("//", StringComparison.Ordinal) && !uri.StartsWith("/\\", StringComparison.Ordinal)
            ? uri
            : "/";
    }

    /// <summary>
    /// Redirects to a local URL (falls back to the home page for external or malformed URLs).
    /// </summary>
    /// <param name="uri">The target.</param>
    public void RedirectTo(string? uri) => navigationManager.NavigateTo(SafeLocal(uri));

    /// <summary>
    /// Redirects to a local page with query parameters.
    /// </summary>
    /// <param name="uri">The target path.</param>
    /// <param name="queryParameters">The query parameters.</param>
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var path = navigationManager.ToAbsoluteUri(SafeLocal(uri)).GetLeftPart(UriPartial.Path);
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameters(path, queryParameters));
    }
}
