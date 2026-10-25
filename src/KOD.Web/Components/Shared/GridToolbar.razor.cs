using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// A GET search form above data grids (works with static rendering and enhanced navigation).
/// Filter <c>&lt;select&gt;</c> elements placed inside can use <c>data-autosubmit</c>.
/// </summary>
public partial class GridToolbar
{
    /// <summary>
    /// Gets or sets the current search term.
    /// </summary>
    [Parameter]
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets the placeholder of the search input.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "Пошук";

    /// <summary>
    /// Gets or sets query parameters that must survive a new search (e.g. the selected tab).
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?> Preserve { get; set; } = new Dictionary<string, string?>();

    /// <summary>
    /// Gets or sets the filter controls.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
