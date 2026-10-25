using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// Redirects anonymous users to the sign-in page, preserving the requested address.
/// </summary>
public partial class RedirectToLogin
{
    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var returnUrl = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        Navigation.NavigateTo($"account/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
