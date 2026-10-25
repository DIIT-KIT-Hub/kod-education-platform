using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Profile;

/// <summary>
/// Tabs of the profile section.
/// </summary>
public partial class ProfileTabs
{
    /// <summary>
    /// Gets or sets the current tab: <c>overview</c>, <c>security</c> or <c>sessions</c>.
    /// </summary>
    [Parameter]
    public string Current { get; set; } = "overview";
}
