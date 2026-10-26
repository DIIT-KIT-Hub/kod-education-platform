using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// Switches the interface language (posts to <c>/account/language</c> and returns to the current page).
/// </summary>
public partial class LanguageSwitch
{
    /// <summary>
    /// Gets or sets additional CSS classes of the form.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets the current page as a local URL.
    /// </summary>
    private string ReturnUrl => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
}
