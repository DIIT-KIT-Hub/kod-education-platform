using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A small status label.
/// </summary>
public partial class Badge
{
    /// <summary>
    /// Gets or sets the tone.
    /// </summary>
    [Parameter]
    public Tone Tone { get; set; }

    /// <summary>
    /// Gets or sets the leading icon name.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a pulsing dot is shown (live states).
    /// </summary>
    [Parameter]
    public bool Dot { get; set; }

    /// <summary>
    /// Gets or sets the tooltip.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => string.Join(' ', new[]
    {
        "ds-badge",
        Tone switch
        {
            Tone.Inverse => "ds-badge--inverse",
            Tone.Outline => "ds-badge--outline",
            Tone.Dashed => "ds-badge--dashed",
            Tone.Danger => "ds-badge--danger",
            _ => null,
        },
        Dot ? "ds-badge--dot" : null,
    }.Where(static c => c is not null));
}
