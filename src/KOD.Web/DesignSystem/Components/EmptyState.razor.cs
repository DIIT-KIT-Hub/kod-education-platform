using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A friendly placeholder for empty lists and search results.
/// </summary>
public partial class EmptyState
{
    /// <summary>
    /// Gets or sets the icon name.
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "sparkles";

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = Texts.T("Тут поки порожньо");

    /// <summary>
    /// Gets or sets the explanation.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the actions.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
