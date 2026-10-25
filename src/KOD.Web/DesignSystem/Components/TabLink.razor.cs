using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A tab of a <see cref="Tabs"/> bar.
/// </summary>
public partial class TabLink
{
    /// <summary>
    /// Gets or sets the link target.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the tab is selected.
    /// </summary>
    [Parameter]
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the counter shown next to the label.
    /// </summary>
    [Parameter]
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the icon name.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
