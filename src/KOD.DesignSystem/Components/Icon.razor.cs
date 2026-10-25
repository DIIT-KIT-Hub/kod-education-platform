using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// An icon from the design system sprite (Lucide icon set).
/// </summary>
public partial class Icon
{
    /// <summary>
    /// Gets or sets the icon name (e.g. <c>file-text</c>).
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon size.
    /// </summary>
    [Parameter]
    public IconSize Size { get; set; }

    /// <summary>
    /// Gets or sets an accessible title; decorative icons omit it.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the icon spins (loading indicators).
    /// </summary>
    [Parameter]
    public bool Spin { get; set; }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => string.Join(' ', new[]
    {
        "ds-icon",
        Size switch
        {
            IconSize.Small => "ds-icon--sm",
            IconSize.Large => "ds-icon--lg",
            IconSize.ExtraLarge => "ds-icon--xl",
            _ => null,
        },
        Spin ? "ds-icon--spin" : null,
        Class,
    }.Where(static c => c is not null));
}
