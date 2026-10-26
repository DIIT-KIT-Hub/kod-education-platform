using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.Roles;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// Creates or edits a role and its permission set (interactive server component).
/// </summary>
public partial class RoleEditor
{
    /// <summary>
    /// Gets or sets the role identifier; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the loaded role. Persisted from prerendering, so the interactive render shows the same form at once
    /// instead of a skeleton while loading it again.
    /// </summary>
    [PersistentState]
    public RoleDetails? Existing { get; set; }

    /// <summary>
    /// Gets or sets the role service.
    /// </summary>
    [Inject]
    private IRoleService Roles { get; set; } = default!;

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
    private RoleModel Model { get; set; } = new();

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

    /// <summary>
    /// Gets a value indicating whether the permissions are read-only (administrator role).
    /// </summary>
    private bool ReadOnly => Existing?.IsAdministrator == true;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Id is { } id)
        {
            if (Existing?.Id != id)
            {
                var result = await Roles.GetAsync(id, CancellationToken.None);
                if (!result.IsSuccess)
                {
                    Navigation.NavigateTo("/admin/roles");
                    return;
                }

                Existing = result.Value;
            }

            Model = new RoleModel
            {
                Code = Existing.Code,
                Name = Existing.Name,
                Description = Existing.Description,
                IsLeadership = Existing.IsLeadership,
                Permissions = [.. Existing.Permissions],
            };
        }

        Loaded = true;
    }

    /// <summary>
    /// Adds or removes a permission.
    /// </summary>
    /// <param name="code">The permission code.</param>
    /// <param name="granted">Whether the permission is granted.</param>
    private void Toggle(string code, bool granted)
    {
        if (granted)
        {
            Model.Permissions.Add(code);
        }
        else
        {
            Model.Permissions.Remove(code);
        }
    }

    /// <summary>
    /// Saves the role.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        Busy = true;
        try
        {
            var command = new SaveRoleCommand(Model.Code, Model.Name, Model.Description, [.. Model.Permissions], Model.IsLeadership);
            if (Id is { } id)
            {
                var result = await Roles.UpdateAsync(id, command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }
            else
            {
                var result = await Roles.CreateAsync(command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }

            if (ErrorMessage is null)
            {
                Navigation.NavigateTo($"/admin/roles?toast={Uri.EscapeDataString(Texts.T("Роль збережено"))}");
            }
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Deletes the role after confirmation.
    /// </summary>
    /// <returns>A task that completes when deleted.</returns>
    private async Task DeleteAsync()
    {
        if (Id is not { } id || !await JS.InvokeAsync<bool>("confirm", Texts.T("Видалити роль?")))
        {
            return;
        }

        var result = await Roles.DeleteAsync(id, CancellationToken.None);
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/admin/roles?toast={Uri.EscapeDataString(Texts.T("Роль видалено"))}");
            return;
        }

        ErrorMessage = result.Error.Message;
    }

    /// <summary>
    /// The role form.
    /// </summary>
    private sealed class RoleModel
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть код ролі.")]
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Лише малі латинські літери, цифри та «-».")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть назву ролі.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the role is a leadership position.
        /// </summary>
        public bool IsLeadership { get; set; }

        /// <summary>
        /// Gets or sets the granted permissions.
        /// </summary>
        public HashSet<string> Permissions { get; set; } = new(StringComparer.Ordinal);
    }
}
