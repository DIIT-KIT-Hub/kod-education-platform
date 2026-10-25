using System.ComponentModel.DataAnnotations;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Web.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Account;

/// <summary>
/// The second sign-in step: a TOTP code from the authenticator app
/// (<see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/>).
/// Users without an enrolled authenticator are sent to the enrollment page.
/// </summary>
public partial class LoginTwoFactor
{
    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm]
    private CodeModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the address to return to.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is persistent.
    /// </summary>
    [SupplyParameterFromQuery]
    private bool RememberMe { get; set; }

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
    /// Gets the link to the recovery code page.
    /// </summary>
    private string RecoveryHref => $"/account/login-recovery?returnUrl={Uri.EscapeDataString(IdentityRedirectManager.SafeLocal(ReturnUrl))}&rememberMe={RememberMe}";

    /// <summary>
    /// Gets or sets the user who passed the password step.
    /// </summary>
    private User? PendingUser { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Input ??= new CodeModel();
        PendingUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (PendingUser is null)
        {
            Redirect.RedirectTo("/account/login");
        }
        else if (!PendingUser.TwoFactorEnabled)
        {
            Redirect.RedirectTo("/account/setup-authenticator", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = RememberMe });
        }
    }

    /// <summary>
    /// Verifies the code and completes the sign-in.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task VerifyAsync()
    {
        if (PendingUser is null)
        {
            return;
        }

        var code = Input.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var result = await SignInManager.TwoFactorAuthenticatorSignInAsync(code, RememberMe, rememberClient: false);
        if (result.Succeeded)
        {
            PendingUser.RecordSignIn(TimeProvider.GetUtcNow());
            await UserManager.UpdateAsync(PendingUser);
            await Audit.WriteAsync(AuditActions.SignIn, PendingUser.Id, PendingUser.FullName, "Пароль + автентифікатор", CancellationToken.None);
            Redirect.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            await Audit.WriteAsync(AuditActions.LockedOut, PendingUser.Id, PendingUser.FullName, null, CancellationToken.None);
            Redirect.RedirectTo("/account/lockout");
        }
        else
        {
            await Audit.WriteAsync(AuditActions.SignInFailed, PendingUser.Id, PendingUser.FullName, "Неправильний код автентифікатора", CancellationToken.None);
            ErrorMessage = "Код неправильний або вже недійсний. Спробуйте наступний код.";
        }
    }

    /// <summary>
    /// The verification code form.
    /// </summary>
    private sealed class CodeModel
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Required(ErrorMessage = "Введіть код.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Код містить 6 цифр.")]
        public string Code { get; set; } = string.Empty;
    }
}
