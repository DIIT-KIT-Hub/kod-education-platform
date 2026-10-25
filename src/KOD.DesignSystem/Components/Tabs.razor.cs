using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A horizontally scrollable, link-based tab bar (works without interactivity).
/// </summary>
public partial class Tabs
{
    /// <summary>
    /// Gets or sets the accessible name of the tab bar.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Розділи";

    /// <summary>
    /// Gets or sets the <see cref="TabLink"/> items.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
