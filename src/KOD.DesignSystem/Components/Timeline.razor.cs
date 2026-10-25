using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A vertical list of steps (e.g. a document signing route).
/// </summary>
public partial class Timeline
{
    /// <summary>
    /// Gets or sets the <see cref="TimelineItem"/> elements.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
