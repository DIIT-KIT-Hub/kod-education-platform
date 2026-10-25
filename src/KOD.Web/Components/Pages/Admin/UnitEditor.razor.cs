using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Models;
using KOD.Application.Features.Organization;
using KOD.Domain.Organization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// Creates or edits an organization unit (interactive server component).
/// </summary>
public partial class UnitEditor
{
    /// <summary>
    /// Gets or sets the unit identifier; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the parent preselected from the query string.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? Parent { get; set; }

    /// <summary>
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    private UnitModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the unit options.
    /// </summary>
    private IReadOnlyList<UnitOption> Units { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the data is loaded.
    /// </summary>
    private bool Loaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an operation is running.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        Units = await Organization.GetOptionsAsync(CancellationToken.None);
        if (Id is { } id)
        {
            var result = await Organization.GetAsync(id, CancellationToken.None);
            if (!result.IsSuccess)
            {
                Navigation.NavigateTo("/admin/units");
                return;
            }

            var unit = result.Value;
            Model = new UnitModel
            {
                Code = unit.Code,
                Name = unit.Name,
                ShortName = unit.ShortName,
                Type = unit.Type,
                ParentId = unit.ParentId,
                SortOrder = unit.SortOrder,
                IsActive = unit.IsActive,
            };
        }
        else
        {
            Model.ParentId ??= Parent ?? (Units.Count > 0 ? Units[0].Id : null);
        }

        Loaded = true;
    }

    /// <summary>
    /// Saves the unit.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        Busy = true;
        try
        {
            var command = new SaveUnitCommand(Model.Code, Model.Name, Model.ShortName, Model.Type, Model.ParentId, Model.SortOrder, Model.IsActive);
            if (Id is { } id)
            {
                var result = await Organization.UpdateAsync(id, command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }
            else
            {
                var result = await Organization.CreateAsync(command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }

            if (ErrorMessage is null)
            {
                Navigation.NavigateTo($"/admin/units?toast={Uri.EscapeDataString("Підрозділ збережено")}");
            }
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// The unit form.
    /// </summary>
    private sealed class UnitModel
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть код.")]
        [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Лише латинські літери, цифри, «-» та «_».")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть назву.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the abbreviation.
        /// </summary>
        public string? ShortName { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public OrganizationUnitType Type { get; set; } = OrganizationUnitType.Department;

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unit is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
