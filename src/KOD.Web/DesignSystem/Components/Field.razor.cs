using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A form field wrapper: label, control, hint and (via <see cref="ChildContent"/>) validation message.
/// </summary>
public partial class Field
{
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the id of the labelled control.
    /// </summary>
    [Parameter]
    public string? For { get; set; }

    /// <summary>
    /// Gets or sets the hint shown under the control.
    /// </summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is required (shows an asterisk).
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes (e.g. <c>ds-span-full</c>).
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the control and its validation message.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
