using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// An inline message block (information, success or error).
/// </summary>
public partial class Alert
{
    /// <summary>
    /// Gets or sets the tone.
    /// </summary>
    [Parameter]
    public Tone Tone { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the icon name; a tone-specific default is used when empty.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the default icon of the tone.
    /// </summary>
    private string DefaultIcon => Tone switch
    {
        Tone.Danger => "circle-alert",
        Tone.Inverse => "circle-check",
        _ => "info",
    };

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => Tone switch
    {
        Tone.Inverse => "ds-alert ds-alert--inverse",
        Tone.Outline or Tone.Dashed => "ds-alert ds-alert--outline",
        Tone.Danger => "ds-alert ds-alert--danger",
        _ => "ds-alert",
    };
}
