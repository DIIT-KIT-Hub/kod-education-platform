using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A paginated, sortable data grid. Paging and sorting are driven by the query string
/// (<c>page</c>, <c>pageSize</c>, <c>sortBy</c>, <c>descending</c>), so it works both with static server rendering and interactivity.
/// Below the tablet breakpoint every row is displayed as a card.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public partial class DataGrid<TItem>
{
    /// <summary>
    /// The registered columns in order.
    /// </summary>
    private readonly List<GridColumn<TItem>> _columns = [];

    /// <summary>
    /// Whether columns are being collected during the current render.
    /// </summary>
    private bool _collectingColumns;

    /// <summary>
    /// Gets or sets the items of the current page.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the 1-based page number.
    /// </summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    [Parameter]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pagination bar is shown.
    /// </summary>
    [Parameter]
    public bool ShowPagination { get; set; } = true;

    /// <summary>
    /// Gets or sets the current sort key.
    /// </summary>
    [Parameter]
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current sort is descending.
    /// </summary>
    [Parameter]
    public bool Descending { get; set; }

    /// <summary>
    /// Gets or sets the accessible caption of the table.
    /// </summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>
    /// Gets or sets the column definitions (<see cref="GridColumn{TItem}"/>).
    /// </summary>
    [Parameter]
    public RenderFragment? Columns { get; set; }

    /// <summary>
    /// Gets or sets the empty state icon.
    /// </summary>
    [Parameter]
    public string EmptyIcon { get; set; } = "search";

    /// <summary>
    /// Gets or sets the empty state title.
    /// </summary>
    [Parameter]
    public string EmptyTitle { get; set; } = "Нічого не знайдено";

    /// <summary>
    /// Gets or sets the empty state text.
    /// </summary>
    [Parameter]
    public string? EmptyText { get; set; } = "Спробуйте змінити параметри пошуку або фільтри.";

    /// <summary>
    /// Gets or sets custom empty state content.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Registers a column while the grid collects its columns.
    /// </summary>
    /// <param name="column">The column.</param>
    internal void AddColumn(GridColumn<TItem> column)
    {
        if (_collectingColumns)
        {
            _columns.Add(column);
        }
    }

    /// <summary>
    /// Starts a new column collection pass.
    /// </summary>
    private void StartCollectingColumns()
    {
        _columns.Clear();
        _collectingColumns = true;
    }

    /// <summary>
    /// Ends the column collection pass.
    /// </summary>
    private void FinishCollectingColumns() => _collectingColumns = false;

    /// <summary>
    /// Builds the link that sorts by the column (toggling the direction when already sorted by it).
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The URL.</returns>
    private string SortHref(GridColumn<TItem> column)
    {
        var descending = string.Equals(SortBy, column.SortKey, StringComparison.Ordinal) && !Descending;
        return Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["sortBy"] = column.SortKey,
            ["descending"] = descending ? true : null,
            ["page"] = null,
        });
    }

    /// <summary>
    /// Returns the sort indicator icon of a column.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The icon name.</returns>
    private string SortIcon(GridColumn<TItem> column) => !string.Equals(SortBy, column.SortKey, StringComparison.Ordinal)
        ? "arrow-up-down"
        : Descending ? "arrow-down" : "arrow-up";

    /// <summary>
    /// Returns the <c>aria-sort</c> value of a column header.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The attribute value or <see langword="null"/>.</returns>
    private string? AriaSort(GridColumn<TItem> column) => column.SortKey is not null && string.Equals(SortBy, column.SortKey, StringComparison.Ordinal)
        ? Descending ? "descending" : "ascending"
        : null;
}
