using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.Profile;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using KOD.Web.Identity;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Account;

/// <summary>
/// Changes the password. Users with a temporary password are redirected here until they set their own one.
/// </summary>
public partial class ChangePassword
{
    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm]
    private PasswordModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the address to return to.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTTP context.
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the profile service.
    /// </summary>
    [Inject]
    private IProfileService Profile { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sign-in manager.
    /// </summary>
    [Inject]
    private SignInManager<User> SignInManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user manager.
    /// </summary>
    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the redirect manager.
    /// </summary>
    [Inject]
    private IdentityRedirectManager Redirect { get; set; } = default!;

    /// <summary>
    /// Gets or sets the error shown above the form.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the change is mandatory (temporary password).
    /// </summary>
    private bool IsForced => HttpContext.User.MustChangePassword;

    /// <inheritdoc />
    protected override void OnInitialized() => Input ??= new PasswordModel();

    /// <summary>
    /// Changes the password and refreshes the sign-in cookie.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task ChangeAsync()
    {
        var result = await Profile.ChangePasswordAsync(Input.CurrentPassword, Input.NewPassword, CancellationToken.None);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        var user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is not null)
        {
            await SignInManager.RefreshSignInAsync(user);
        }

        var target = IdentityRedirectManager.SafeLocal(ReturnUrl);
        Redirect.RedirectTo(target, new() { ["toast"] = Texts.T("Пароль змінено") });
    }

    /// <summary>
    /// The password change form.
    /// </summary>
    private sealed class PasswordModel
    {
        /// <summary>
        /// Gets or sets the current password.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть поточний пароль.")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть новий пароль.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Пароль має містити від 12 до 128 символів.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation.
        /// </summary>
        [Compare(nameof(NewPassword), ErrorMessage = "Паролі не збігаються.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
