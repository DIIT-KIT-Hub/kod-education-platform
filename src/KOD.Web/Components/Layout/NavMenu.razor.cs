using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Layout;

/// <summary>
/// The vertical list of main navigation links.
/// </summary>
public partial class NavMenu
{
    /// <summary>
    /// Gets or sets the visible entries.
    /// </summary>
    [Parameter]
    public IReadOnlyList<NavigationItem> Items { get; set; } = [];
}
