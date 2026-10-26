using System.ComponentModel.DataAnnotations;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using KOD.Web.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Account;

/// <summary>
/// Signs in with a one-time recovery code instead of the authenticator
/// (<see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>).
/// </summary>
public partial class LoginRecovery
{
    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm]
    private RecoveryModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the address to return to.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

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
    /// Gets or sets the audit logger.
    /// </summary>
    [Inject]
    private IAuditLogger Audit { get; set; } = default!;

    /// <summary>
    /// Gets or sets the clock.
    /// </summary>
    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

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
    /// Gets or sets the user who passed the password step.
    /// </summary>
    private User? PendingUser { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Input ??= new RecoveryModel();
        PendingUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (PendingUser is null || !PendingUser.TwoFactorEnabled)
        {
            Redirect.RedirectTo("/account/login");
        }
    }

    /// <summary>
    /// Redeems the code and completes the sign-in.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task VerifyAsync()
    {
        if (PendingUser is null)
        {
            return;
        }

        var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(Input.Code.Replace(" ", string.Empty, StringComparison.Ordinal));
        if (result.Succeeded)
        {
            PendingUser.RecordSignIn(TimeProvider.GetUtcNow());
            await UserManager.UpdateAsync(PendingUser);
            await Audit.WriteAsync(AuditActions.SignIn, PendingUser.Id, PendingUser.FullName, "Пароль + резервний код", CancellationToken.None);
            Redirect.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            Redirect.RedirectTo("/account/lockout");
        }
        else
        {
            await Audit.WriteAsync(AuditActions.SignInFailed, PendingUser.Id, PendingUser.FullName, "Неправильний резервний код", CancellationToken.None);
            ErrorMessage = Texts.T("Резервний код недійсний.");
        }
    }

    /// <summary>
    /// The recovery code form.
    /// </summary>
    private sealed class RecoveryModel
    {
        /// <summary>
        /// Gets or sets the recovery code.
        /// </summary>
        [Required(ErrorMessage = "Введіть резервний код.")]
        public string Code { get; set; } = string.Empty;
    }
}
