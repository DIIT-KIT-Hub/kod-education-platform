using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A rounded surface that groups related content. Becomes a clickable card when <see cref="Href"/> is set.
/// </summary>
public partial class Card
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the subtitle shown under the title.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets content shown at the right side of the header.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the footer content.
    /// </summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Gets or sets the visual style.
    /// </summary>
    [Parameter]
    public CardVariant Variant { get; set; }

    /// <summary>
    /// Gets or sets the link target of a clickable card.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets any other HTML attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => string.Join(' ', new[]
    {
        "ds-card",
        Variant switch
        {
            CardVariant.Muted => "ds-card--muted",
            CardVariant.Inverse => "ds-card--inverse",
            CardVariant.Flush => "ds-card--flush",
            _ => null,
        },
        Href is not null ? "ds-card--interactive" : null,
        Class,
    }.Where(static c => c is not null));
}
