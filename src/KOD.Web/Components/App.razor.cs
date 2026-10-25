using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components;

/// <summary>
/// The HTML document shell. Rendered statically on every request; interactive islands declare their own render mode.
/// </summary>
public partial class App
{
    /// <summary>
    /// Gets or sets the HTTP context of the request.
    /// </summary>
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    /// <summary>
    /// Gets or sets the antiforgery service.
    /// </summary>
    [Inject]
    private IAntiforgery Antiforgery { get; set; } = default!;

    /// <summary>
    /// Gets or sets the antiforgery request token for <c>fetch</c> calls to the REST API (sent as the <c>X-CSRF-TOKEN</c> header).
    /// </summary>
    private string? CsrfToken { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (HttpContext is not null)
        {
            CsrfToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
        }
    }
}
