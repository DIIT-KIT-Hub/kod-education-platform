using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Placeholder cards (statistics, news, lists of cards) shown while their content loads.
/// </summary>
public partial class CardSkeleton
{
    /// <summary>
    /// Gets or sets the number of cards.
    /// </summary>
    [Parameter]
    public int Count { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of text lines under the title.
    /// </summary>
    [Parameter]
    public int Lines { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether the cards start with a 16:9 cover image.
    /// </summary>
    [Parameter]
    public bool Media { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cards start with an icon tile (statistics).
    /// </summary>
    [Parameter]
    public bool Icon { get; set; }

    /// <summary>
    /// Gets or sets the CSS classes of the container that lays the cards out (e.g. <c>ds-cols</c>, <c>news-grid</c>).
    /// </summary>
    [Parameter]
    public string Class { get; set; } = "ds-cols";
}
