using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A key figure tile for dashboards.
/// </summary>
public partial class StatCard
{
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon name.
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "layers";

    /// <summary>
    /// Gets or sets the link to the details.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tile is highlighted (inverse colors).
    /// </summary>
    [Parameter]
    public bool Inverse { get; set; }
}
