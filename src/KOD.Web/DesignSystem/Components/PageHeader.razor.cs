using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// The page heading with breadcrumbs, subtitle and primary actions. Also sets the document title.
/// </summary>
public partial class PageHeader
{
    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtitle.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumb trail.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BreadcrumbItem>? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets the actions shown at the right side.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Gets or sets additional content under the subtitle (badges, meta).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the product name appended to the document title.
    /// </summary>
    private static string Product => Texts.T("КОД");
}
