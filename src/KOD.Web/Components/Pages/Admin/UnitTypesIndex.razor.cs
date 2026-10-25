using KOD.Application.Features.Organization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The unit type catalog.
/// </summary>
public partial class UnitTypesIndex
{
    /// <summary>
    /// Gets or sets the unit type service.
    /// </summary>
    [Inject]
    private IUnitTypeService UnitTypes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded types.
    /// </summary>
    private IReadOnlyList<UnitTypeItem>? Items { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var result = await UnitTypes.ListAsync(CancellationToken.None);
        Items = result.IsSuccess ? result.Value : [];
    }
}
