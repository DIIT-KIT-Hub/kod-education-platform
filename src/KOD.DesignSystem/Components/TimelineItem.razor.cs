using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A step of a <see cref="Timeline"/>.
/// </summary>
public partial class TimelineItem
{
    /// <summary>
    /// Gets or sets the step state.
    /// </summary>
    [Parameter]
    public TimelineState State { get; set; }

    /// <summary>
    /// Gets or sets the marker text for pending and current steps (usually the step number).
    /// </summary>
    [Parameter]
    public string? Marker { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary text.
    /// </summary>
    [Parameter]
    public string? Meta { get; set; }

    /// <summary>
    /// Gets or sets additional content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => State switch
    {
        TimelineState.Done => "ds-timeline__item ds-timeline__item--done",
        TimelineState.Current => "ds-timeline__item ds-timeline__item--current",
        TimelineState.Failed => "ds-timeline__item ds-timeline__item--failed",
        _ => "ds-timeline__item",
    };
}
