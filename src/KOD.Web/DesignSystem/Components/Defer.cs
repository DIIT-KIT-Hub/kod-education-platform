using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Renders its content after preceding sibling components; used by <see cref="DataGrid{TItem}"/>
/// to render the table only after all <see cref="GridColumn{TItem}"/> children registered themselves.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class Defer : ComponentBase
{
    /// <summary>
    /// Gets or sets the deferred content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddContent(0, ChildContent);
    }
}
