using Microsoft.AspNetCore.Authentication;

namespace KOD.Web.Identity;

/// <summary>
/// Options of the <see cref="JwtAuthenticationHandler"/>.
/// </summary>
internal sealed class JwtAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the sign-in page that unauthenticated page requests are redirected to.
    /// </summary>
    public PathString LoginPath { get; set; } = "/account/login";

    /// <summary>
    /// Gets or sets the page shown when a signed-in user lacks a permission.
    /// </summary>
    public PathString AccessDeniedPath { get; set; } = "/account/access-denied";

    /// <summary>
    /// Gets or sets the query parameter that carries the page to return to.
    /// </summary>
    public string ReturnUrlParameter { get; set; } = "returnUrl";

    /// <summary>
    /// Gets or sets the path prefix of the REST API: its clients get 401/403 instead of redirects.
    /// </summary>
    public PathString ApiPath { get; set; } = "/api";
}
