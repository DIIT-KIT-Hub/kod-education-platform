using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Application.Features.Organization;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// Creates or edits a user account: personal data, positions, individual permissions and security actions
/// (interactive server component).
/// </summary>
public partial class UserEditor
{
    /// <summary>
    /// Gets or sets the user identifier; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the loaded user. Persisted from prerendering (like the option lists), so the interactive render
    /// shows the same form at once instead of a skeleton while loading it again.
    /// </summary>
    [PersistentState]
    public UserDetails? Details { get; set; }

    /// <summary>
    /// Gets or sets the unit options; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UnitOption>? UnitOptions { get; set; }

    /// <summary>
    /// Gets or sets the role options; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<RoleOption>? RoleOptions { get; set; }

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user service.
    /// </summary>
    [Inject]
    private IUserService Users { get; set; } = default!;

    /// <summary>
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the role service.
    /// </summary>
    [Inject]
    private IRoleService RoleService { get; set; } = default!;

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
    private UserModel Model { get; set; } = new();

    /// <summary>
    /// Gets the unit options.
    /// </summary>
    private IReadOnlyList<UnitOption> Units => UnitOptions ?? [];

    /// <summary>
    /// Gets the role options.
    /// </summary>
    private IReadOnlyList<RoleOption> Roles => RoleOptions ?? [];

    /// <summary>
    /// Gets or sets the effective permissions of the user.
    /// </summary>
    private IReadOnlySet<string> Effective { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets one-time credentials to show.
    /// </summary>
    private UserCredentials? Credentials { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the data is loaded.
    /// </summary>
    private bool Loaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an operation is running.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets an informational message.
    /// </summary>
    private string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can edit accounts.
    /// </summary>
    private bool CanManage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can reset credentials.
    /// </summary>
    private bool CanSecurity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can set individual permissions.
    /// </summary>
    private bool CanManageRoles { get; set; }

    /// <summary>
    /// Gets a value indicating whether the account may be changed as a whole: personal data, e-mail, status, individual permissions.
    /// </summary>
    private bool CanEditAccount => Details?.CanEditAccount ?? (Id is null && CanManage);

    /// <summary>
    /// Gets a value indicating whether the positions in the current user's organizations may be changed.
    /// </summary>
    private bool CanEditPositions => Details?.CanEditPositions ?? (Id is null && CanManage);

    /// <summary>
    /// Gets a value indicating whether the main position is held in another organization and therefore cannot be moved.
    /// </summary>
    private bool PrimaryIsForeign => Model.Positions.Exists(static p => !p.IsManaged && p.IsPrimary);

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var user = (await AuthenticationState).User;
        CanManage = user.HasPermission(Permissions.Users.Manage);
        CanSecurity = user.HasPermission(Permissions.Users.Security);
        CanManageRoles = user.HasPermission(Permissions.Roles.Manage);
        UnitOptions ??= await Organization.GetOptionsAsync(Permissions.Users.Manage, CancellationToken.None);
        RoleOptions ??= await RoleService.GetOptionsAsync(CancellationToken.None);
        if (Details is { } restored && restored.Id == Id)
        {
            Show(restored);
        }
        else
        {
            await LoadAsync();
        }

        Loaded = true;
    }

    /// <summary>
    /// Loads the user into the form.
    /// </summary>
    /// <returns>A task that completes when loaded.</returns>
    private async Task LoadAsync()
    {
        if (Id is not { } id)
        {
            return;
        }

        var result = await Users.GetAsync(id, CancellationToken.None);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        Show(result.Value);
    }

    /// <summary>
    /// Shows a loaded user in the form.
    /// </summary>
    /// <param name="details">The user.</param>
    private void Show(UserDetails details)
    {
        Details = details;
        Effective = Details.EffectivePermissions;
        Model = new UserModel
        {
            Email = Details.Email,
            LastName = Details.LastName,
            FirstName = Details.FirstName,
            MiddleName = Details.MiddleName,
            PhoneNumber = Details.PhoneNumber,
            Positions =
            [
                .. Details.Positions.Select(static p => new PositionModel
                {
                    UnitId = p.UnitId,
                    RoleId = p.RoleId,
                    Title = p.Title,
                    IsPrimary = p.IsPrimary,
                    IsManaged = p.IsManaged,
                    Description = $"{p.UnitName} · {p.Organization.Name} · {p.RoleName}",
                }),
            ],
            Overrides = Details.GrantedPermissions.Select(static p => (p, "grant"))
                .Concat(Details.DeniedPermissions.Select(static p => (p, "deny")))
                .ToDictionary(static x => x.p, static x => x.Item2),
        };
    }

    /// <summary>
    /// Adds an empty position row.
    /// </summary>
    private void AddPosition() => Model.Positions.Add(new PositionModel { IsPrimary = Model.Positions.Count == 0 });

    /// <summary>
    /// Removes a position row.
    /// </summary>
    /// <param name="position">The position.</param>
    private void RemovePosition(PositionModel position) => Model.Positions.Remove(position);

    /// <summary>
    /// Makes a position the main one.
    /// </summary>
    /// <param name="position">The position.</param>
    private void SetPrimary(PositionModel position) => Model.Positions.ForEach(p => p.IsPrimary = ReferenceEquals(p, position));

    /// <summary>
    /// Returns the override state of a permission.
    /// </summary>
    /// <param name="code">The permission code.</param>
    /// <returns><c>inherit</c>, <c>grant</c> or <c>deny</c>.</returns>
    private string OverrideOf(string code) => Model.Overrides.GetValueOrDefault(code, "inherit");

    /// <summary>
    /// Sets the override state of a permission.
    /// </summary>
    /// <param name="code">The permission code.</param>
    /// <param name="value">The new state.</param>
    private void SetOverride(string code, string? value)
    {
        if (value is "grant" or "deny")
        {
            Model.Overrides[code] = value;
        }
        else
        {
            Model.Overrides.Remove(code);
        }
    }

    /// <summary>
    /// Creates or updates the account.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        if (Model.Positions.Exists(static p => p.UnitId == Guid.Empty || p.RoleId == Guid.Empty))
        {
            ErrorMessage = Texts.T("Для кожної посади оберіть підрозділ і роль.");
            return;
        }

        var command = new SaveUserCommand(
            Model.Email,
            Model.LastName,
            Model.FirstName,
            Model.MiddleName,
            Model.PhoneNumber,
            [.. Model.Positions.Select(static p => new PositionInput(p.UnitId, p.RoleId, p.Title, p.IsPrimary))],
            [.. Model.Overrides.Where(static o => o.Value == "grant").Select(static o => o.Key)],
            [.. Model.Overrides.Where(static o => o.Value == "deny").Select(static o => o.Key)]);

        await RunAsync(async () =>
        {
            if (Id is { } id)
            {
                var result = await Users.UpdateAsync(id, command, CancellationToken.None);
                if (result.IsSuccess)
                {
                    await LoadAsync();
                }

                return (result, Texts.T("Зміни збережено."));
            }

            var created = await Users.CreateAsync(command, CancellationToken.None);
            if (!created.IsSuccess)
            {
                return (Result.Failure(created.Error), null);
            }

            Credentials = created.Value;
            Id = created.Value.UserId;
            Navigation.NavigateTo($"/admin/users/{created.Value.UserId}", replace: true);
            await LoadAsync();
            return (Result.Success(), Texts.T("Обліковий запис створено."));
        });
    }

    /// <summary>
    /// Blocks or unblocks the account.
    /// </summary>
    /// <returns>A task that completes when done.</returns>
    private async Task ToggleActiveAsync()
    {
        if (Details is null || !await ConfirmAsync(Details.IsActive ? Texts.T("Заблокувати обліковий запис? Усі сесії буде завершено.") : Texts.T("Розблокувати обліковий запис?")))
        {
            return;
        }

        var activate = !Details.IsActive;
        await RunAsync(async () => (await Users.SetActiveAsync(Details.Id, activate, CancellationToken.None), activate ? Texts.T("Обліковий запис розблоковано.") : Texts.T("Обліковий запис заблоковано.")));
        await LoadAsync();
    }

    /// <summary>
    /// Issues a temporary password.
    /// </summary>
    /// <returns>A task that completes when done.</returns>
    private async Task ResetPasswordAsync()
    {
        if (Details is null || !await ConfirmAsync(Texts.T("Видати новий тимчасовий пароль? Поточний пароль перестане діяти, а сесії буде завершено.")))
        {
            return;
        }

        await RunAsync(async () =>
        {
            var result = await Users.ResetPasswordAsync(Details.Id, CancellationToken.None);
            Credentials = result.IsSuccess ? result.Value : null;
            return (result.IsSuccess ? Result.Success() : Result.Failure(result.Error), Texts.T("Тимчасовий пароль видано."));
        });
        await LoadAsync();
    }

    /// <summary>
    /// Resets the authenticator.
    /// </summary>
    /// <returns>A task that completes when done.</returns>
    private async Task ResetAuthenticatorAsync()
    {
        if (Details is null || !await ConfirmAsync(Texts.T("Скинути автентифікатор? Під час наступного входу користувач прив'яже новий.")))
        {
            return;
        }

        await RunAsync(async () => (await Users.ResetAuthenticatorAsync(Details.Id, CancellationToken.None), Texts.T("Автентифікатор скинуто.")));
        await LoadAsync();
    }

    /// <summary>
    /// Ends all sessions of the user.
    /// </summary>
    /// <returns>A task that completes when done.</returns>
    private async Task RevokeSessionsAsync()
    {
        if (Details is null || !await ConfirmAsync(Texts.T("Завершити всі сесії користувача?")))
        {
            return;
        }

        await RunAsync(async () => (await Users.RevokeSessionsAsync(Details.Id, CancellationToken.None), Texts.T("Сесії завершено.")));
        await LoadAsync();
    }

    /// <summary>
    /// Asks the user to confirm an action.
    /// </summary>
    /// <param name="message">The question.</param>
    /// <returns><see langword="true"/> when confirmed.</returns>
    private ValueTask<bool> ConfirmAsync(string message) => JS.InvokeAsync<bool>("confirm", message);

    /// <summary>
    /// Runs an operation with the busy indicator and shows its outcome.
    /// </summary>
    /// <param name="operation">The operation returning a result and the success message.</param>
    /// <returns>A task that completes when the operation ends.</returns>
    private async Task RunAsync(Func<Task<(Result Result, string? Success)>> operation)
    {
        Busy = true;
        Message = null;
        ErrorMessage = null;
        try
        {
            var (result, success) = await operation();
            Message = result.IsSuccess ? success : null;
            ErrorMessage = result.IsSuccess ? null : result.Error.Message;
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// The user form.
    /// </summary>
    private sealed class UserModel
    {
        /// <summary>
        /// Gets or sets the e-mail.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Некоректна адреса.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть прізвище.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть ім'я.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patronymic.
        /// </summary>
        public string? MiddleName { get; set; }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the positions.
        /// </summary>
        public List<PositionModel> Positions { get; set; } = [];

        /// <summary>
        /// Gets or sets the individual permission overrides (code → grant/deny).
        /// </summary>
        public Dictionary<string, string> Overrides { get; set; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// A position row.
    /// </summary>
    private sealed class PositionModel
    {
        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        public Guid UnitId { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the main position.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the position is in the current user's organizations (otherwise it is read-only).
        /// </summary>
        public bool IsManaged { get; set; } = true;

        /// <summary>
        /// Gets or sets the unit, organization and role of a stored position, shown for read-only positions.
        /// </summary>
        public string? Description { get; set; }
    }
}
