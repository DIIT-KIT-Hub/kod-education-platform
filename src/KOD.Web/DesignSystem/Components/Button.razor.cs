using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A pill-shaped button. Renders an <c>&lt;a&gt;</c> when <see cref="Href"/> is set, otherwise a <c>&lt;button&gt;</c>.
/// </summary>
public partial class Button
{
    /// <summary>
    /// Gets or sets the visual style.
    /// </summary>
    [Parameter]
    public ButtonVariant Variant { get; set; }

    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    [Parameter]
    public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>
    /// Gets or sets the link target; turns the button into a link.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the button type (<c>button</c>, <c>submit</c>, <c>reset</c>).
    /// </summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>
    /// Gets or sets the leading icon name.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the trailing icon name.
    /// </summary>
    [Parameter]
    public string? IconEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button stretches to the container width.
    /// </summary>
    [Parameter]
    public bool Block { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button is square (icon only).
    /// </summary>
    [Parameter]
    public bool IconOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the click handler (interactive render modes only).
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets any other HTML attributes (e.g. <c>name</c>, <c>value</c>, <c>form</c>, <c>aria-label</c>).
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => string.Join(' ', new[]
    {
        "ds-btn",
        Variant switch
        {
            ButtonVariant.Secondary => "ds-btn--secondary",
            ButtonVariant.Ghost => "ds-btn--ghost",
            ButtonVariant.Danger => "ds-btn--danger",
            _ => null,
        },
        Size switch
        {
            ControlSize.Small => "ds-btn--sm",
            ControlSize.Large => "ds-btn--lg",
            _ => null,
        },
        Block ? "ds-btn--block" : null,
        IconOnly ? "ds-btn--icon" : null,
        Class,
    }.Where(static c => c is not null));
}
