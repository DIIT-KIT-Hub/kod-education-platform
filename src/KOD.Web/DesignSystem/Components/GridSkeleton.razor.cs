using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// The placeholder of a <see cref="DataGrid{TItem}"/> while its page loads: a table (cards on phones) of shimmering bars
/// with uneven, but stable, widths.
/// </summary>
public partial class GridSkeleton
{
    /// <summary>
    /// Gets or sets the number of rows.
    /// </summary>
    [Parameter]
    public int Rows { get; set; } = 6;

    /// <summary>
    /// Gets or sets the number of columns.
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 5;

    /// <summary>
    /// Returns a stable pseudo-random bar width.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="min">The minimum width (rem).</param>
    /// <param name="max">The maximum width (rem).</param>
    /// <returns>The width in rem, formatted for CSS.</returns>
    private static string Width(int column, int row, double min, double max)
    {
        var seed = (((column * 7) + (row * 13) + 3) % 10) / 9d;
        return (min + ((max - min) * seed)).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
