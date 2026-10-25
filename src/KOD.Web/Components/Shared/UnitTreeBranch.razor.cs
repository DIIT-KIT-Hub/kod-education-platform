using KOD.Application.Features.Organization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// A recursive, collapsible branch of the organization tree (native <c>&lt;details&gt;</c>, no JavaScript).
/// </summary>
public partial class UnitTreeBranch
{
    /// <summary>
    /// Gets or sets the node.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public UnitTreeNode Node { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the branch is initially expanded.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }
}
