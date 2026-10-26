using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Adds a show / hide button to a password input: <c>&lt;PasswordReveal&gt;&lt;InputText type="password" … /&gt;&lt;/PasswordReveal&gt;</c>.
/// The input keeps its bindings; the script only switches its <c>type</c>, and hides the password again when the form submits.
/// </summary>
public partial class PasswordReveal
{
    /// <summary>
    /// Gets or sets the password input.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
