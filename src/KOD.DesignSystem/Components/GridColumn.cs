using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace KOD.DesignSystem.Components;

/// <summary>
/// A column of a <see cref="DataGrid{TItem}"/>. Renders nothing itself; it registers with the parent grid.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class GridColumn<TItem> : ComponentBase
{
    /// <summary>
    /// Gets or sets the header text (also used as the label of the cell in the mobile card layout).
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort key sent in the <c>sortBy</c> query parameter; the column is not sortable when empty.
    /// </summary>
    [Parameter]
    public string? SortKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary column (shown as the card title on phones).
    /// </summary>
    [Parameter]
    public bool Primary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the trailing actions column.
    /// </summary>
    [Parameter]
    public bool Actions { get; set; }

    /// <summary>
    /// Gets or sets the CSS classes of the cells.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the cell template.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<TItem> ChildContent { get; set; } = static _ => static _ => { };

    /// <summary>
    /// Gets or sets the owning grid.
    /// </summary>
    [CascadingParameter]
    internal DataGrid<TItem>? Grid { get; set; }

    /// <summary>
    /// Gets the CSS classes of the cells.
    /// </summary>
    internal string? CellClass => Actions ? $"ds-col-actions {Class}".Trim() : Class;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) => Grid?.AddColumn(this);
}
