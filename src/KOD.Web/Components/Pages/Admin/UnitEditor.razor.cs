using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Models;
using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

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
    /// Gets or sets the edited unit. Persisted from prerendering (like the option lists), so the interactive render
    /// shows the same form at once instead of a skeleton while loading it again.
    /// </summary>
    [PersistentState]
    public UnitDetails? Existing { get; set; }

    /// <summary>
    /// Gets or sets the unit options; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UnitOption>? UnitOptions { get; set; }

    /// <summary>
    /// Gets or sets the unit types; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UnitTypeRef>? TypeOptions { get; set; }

    /// <summary>
    /// Gets or sets the parent preselected from the query string.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? Parent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a new top-level organization is created.
    /// </summary>
    [SupplyParameterFromQuery]
    private bool Top { get; set; }

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unit type service.
    /// </summary>
    [Inject]
    private IUnitTypeService UnitTypes { get; set; } = default!;

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
    /// Gets the unit options.
    /// </summary>
    private IReadOnlyList<UnitOption> Units => UnitOptions ?? [];

    /// <summary>
    /// Gets the unit types.
    /// </summary>
    private IReadOnlyList<UnitTypeRef> Types => TypeOptions ?? [];

    /// <summary>
    /// Gets or sets a value indicating whether the current user manages the structure of every organization.
    /// </summary>
    private bool CabinetWide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the form is read-only (the unit is outside the current user's organizations).
    /// </summary>
    private bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the edited unit is a top-level organization.
    /// </summary>
    private bool WasTopLevel { get; set; }

    /// <summary>
    /// Gets a value indicating whether «no parent» (a top-level organization) may be chosen.
    /// </summary>
    private bool CanBeTopLevel => CabinetWide || WasTopLevel;

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
        UnitOptions ??= await Organization.GetOptionsAsync(Permissions.Structure.Manage, CancellationToken.None);
        TypeOptions ??= await UnitTypes.GetOptionsAsync(CancellationToken.None);
        if (Id is { } id)
        {
            if (Existing?.Id != id)
            {
                var result = await Organization.GetAsync(id, CancellationToken.None);
                if (!result.IsSuccess)
                {
                    Navigation.NavigateTo("/admin/units");
                    return;
                }

                Existing = result.Value;
            }

            var unit = Existing;
            WasTopLevel = unit.ParentId is null;
            ReadOnly = !unit.CanManage;
            ErrorMessage = ReadOnly ? Texts.T("Підрозділ належить організації, структурою якої ви не керуєте, тому його можна лише переглянути.") : null;
            Model = new UnitModel
            {
                Code = unit.Code,
                Name = unit.Name,
                ShortName = unit.ShortName,
                TypeId = unit.Type.Id,
                ParentId = unit.ParentId,
                SortOrder = unit.SortOrder,
                IsActive = unit.IsActive,
            };
        }
        else if (Top && !CabinetWide)
        {
            ReadOnly = true;
            ErrorMessage = Texts.T("Організації верхнього рівня створює лише адміністратор усіх організацій. Ви можете додавати підрозділи й дочірні організації у своїх організаціях.");
        }
        else
        {
            Model.ParentId = Top ? null : Units.FirstOrDefault(u => u.Id == Parent)?.Id ?? (Units.Count > 0 ? Units[0].Id : null);
            Model.TypeId ??= Types.FirstOrDefault(t => t.IsOrganization == (Model.ParentId is null))?.Id;
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
            var command = new SaveUnitCommand(Model.Code, Model.Name, Model.ShortName, Model.TypeId ?? Guid.Empty, Model.ParentId, Model.SortOrder, Model.IsActive);
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
                Navigation.NavigateTo($"/admin/units?toast={Uri.EscapeDataString(Texts.T("Підрозділ збережено"))}");
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
        /// Gets or sets the unit type.
        /// </summary>
        [Required(ErrorMessage = "Оберіть тип.")]
        public Guid? TypeId { get; set; }

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
