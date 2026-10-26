using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Domain.Organization;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// Creates, edits or deletes a unit type (interactive server component).
/// </summary>
public partial class UnitTypeEditor
{
    /// <summary>
    /// The icons offered in the picker, in display order.
    /// </summary>
    private static readonly string[] Icons = [.. UnitType.Icons.Order(StringComparer.Ordinal)];

    /// <summary>
    /// Gets or sets the type identifier; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the unit type service.
    /// </summary>
    [Inject]
    private IUnitTypeService UnitTypes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the current user manages the structure of every organization
    /// (top-level organizations and the unit type catalog shared by all of them).
    /// </summary>
    private bool CabinetWide { get; set; }

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    private UnitTypeModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the type being edited.
    /// </summary>
    private UnitTypeItem? Existing { get; set; }

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
        CabinetWide = (await AuthenticationState).User.HasPermission(Permissions.Administration.AllOrganizations);
        if (Id is { } id)
        {
            var result = await UnitTypes.GetAsync(id, CancellationToken.None);
            if (!result.IsSuccess)
            {
                Navigation.NavigateTo("/admin/unit-types");
                return;
            }

            Existing = result.Value;
            Model = new UnitTypeModel
            {
                Code = Existing.Code,
                Name = Existing.Name,
                Description = Existing.Description,
                Icon = Existing.Icon,
                IsOrganization = Existing.IsOrganization,
                SortOrder = Existing.SortOrder,
            };
        }

        Loaded = true;
    }

    /// <summary>
    /// Saves the type.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        Busy = true;
        try
        {
            var command = new SaveUnitTypeCommand(Model.Code, Model.Name, Model.Description, Model.Icon, Model.IsOrganization, Model.SortOrder);
            if (Id is { } id)
            {
                var result = await UnitTypes.UpdateAsync(id, command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }
            else
            {
                var result = await UnitTypes.CreateAsync(command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }

            if (ErrorMessage is null)
            {
                Navigation.NavigateTo($"/admin/unit-types?toast={Uri.EscapeDataString(Texts.T("Тип підрозділу збережено"))}");
            }
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Deletes the type after a confirmation.
    /// </summary>
    /// <returns>A task that completes when deleted.</returns>
    private async Task DeleteAsync()
    {
        if (Id is not { } id || !await JS.InvokeAsync<bool>("confirm", Texts.T("Видалити тип підрозділу?")))
        {
            return;
        }

        var result = await UnitTypes.DeleteAsync(id, CancellationToken.None);
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/admin/unit-types?toast={Uri.EscapeDataString(Texts.T("Тип підрозділу видалено"))}");
            return;
        }

        ErrorMessage = result.Error.Message;
    }

    /// <summary>
    /// The unit type form.
    /// </summary>
    private sealed class UnitTypeModel
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
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the icon name.
        /// </summary>
        public string Icon { get; set; } = UnitType.DefaultIcon;

        /// <summary>
        /// Gets or sets a value indicating whether units of this type are organizations.
        /// </summary>
        public bool IsOrganization { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int SortOrder { get; set; } = 100;
    }
}
