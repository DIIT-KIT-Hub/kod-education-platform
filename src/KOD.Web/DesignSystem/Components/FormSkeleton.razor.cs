using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// The placeholder of an editor page while its data loads: form cards with label and field bars and an action panel.
/// </summary>
public partial class FormSkeleton
{
    /// <summary>
    /// Gets or sets the number of form cards.
    /// </summary>
    [Parameter]
    public int Cards { get; set; } = 2;

    /// <summary>
    /// Gets or sets the number of fields per card.
    /// </summary>
    [Parameter]
    public int Fields { get; set; } = 4;
}
