using Microsoft.AspNetCore.Components;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A modal dialog based on the native <c>&lt;dialog&gt;</c> element.
/// Open it with any element that has <c>data-dialog-open="{Id}"</c>; close it with <c>data-dialog-close</c> or Esc.
/// On phones it slides up as a bottom sheet.
/// </summary>
public partial class Dialog
{
    /// <summary>
    /// Gets or sets the element id.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description under the title.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is wide.
    /// </summary>
    [Parameter]
    public bool Wide { get; set; }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
