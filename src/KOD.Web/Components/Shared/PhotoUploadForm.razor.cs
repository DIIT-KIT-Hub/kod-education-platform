using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// The profile photo form (<c>POST /account/photo</c>) with a square crop preview.
/// </summary>
public partial class PhotoUploadForm
{
    /// <summary>
    /// Gets or sets the local page to open after saving.
    /// </summary>
    [Parameter]
    public string ReturnUrl { get; set; } = "/profile";

    /// <summary>
    /// Gets or sets the text of the submit button.
    /// </summary>
    [Parameter]
    public string? SubmitText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the submit button takes the full width.
    /// </summary>
    [Parameter]
    public bool Block { get; set; }
}
