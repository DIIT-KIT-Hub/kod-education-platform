using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using KOD.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Account;

/// <summary>
/// Mandatory authenticator enrollment at the first sign-in (and after an administrator reset).
/// A user who signed in with a temporary password first replaces it, so the temporary password stops working before
/// an authenticator is bound to the account. Uses the Identity authenticator token provider (TOTP, RFC 6238) and issues
/// recovery codes.
/// </summary>
public partial class SetupAuthenticator
{
    /// <summary>
    /// Gets or sets the enrollment form model.
    /// </summary>
    [SupplyParameterFromForm(FormName = "setup-authenticator")]
    private EnrollModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the first password form model.
    /// </summary>
    [SupplyParameterFromForm(FormName = "first-password")]
    private PasswordModel Password { get; set; } = default!;

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
    /// Gets or sets the HTTP context.
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

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
    /// Gets or sets the shared key formatted for manual entry.
    /// </summary>
    private string? SharedKey { get; set; }

    /// <summary>
    /// Gets or sets the unformatted shared key.
    /// </summary>
    private string? SharedKeyRaw { get; set; }

    /// <summary>
    /// Gets or sets the QR code markup.
    /// </summary>
    private string? QrSvg { get; set; }

    /// <summary>
    /// Gets or sets the recovery codes shown once after a successful enrollment.
    /// </summary>
    private IReadOnlyList<string>? RecoveryCodes { get; set; }

    /// <summary>
    /// Gets or sets the user who passed the password step.
    /// </summary>
    private User? PendingUser { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user must first replace a temporary password.
    /// </summary>
    private bool MustSetPassword => PendingUser?.MustChangePassword == true;

    /// <summary>
    /// Gets or sets a value indicating whether the user started with a temporary password (shows the step counter).
    /// </summary>
    [SupplyParameterFromQuery(Name = "firstLogin")]
    private bool WasTemporaryPassword { get; set; }

    /// <summary>
    /// Gets the address to continue to.
    /// </summary>
    private string ContinueHref => IdentityRedirectManager.SafeLocal(ReturnUrl);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Input ??= new EnrollModel();
        Password ??= new PasswordModel();
        PendingUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (PendingUser is null || !PendingUser.IsActive)
        {
            Redirect.RedirectTo("/account/login");
            return;
        }

        if (PendingUser.TwoFactorEnabled)
        {
            Redirect.RedirectTo("/account/login-2fa", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = RememberMe });
            return;
        }

        if (await UserManager.IsLockedOutAsync(PendingUser))
        {
            Redirect.RedirectTo("/account/lockout");
            return;
        }

        if (!MustSetPassword)
        {
            await PrepareKeyAsync(PendingUser);
        }
    }

    /// <summary>
    /// Replaces the temporary password. The user proved knowledge of it on the sign-in page minutes ago
    /// (the two-factor cookie lives 10 minutes), so the reset-token path of Identity is used.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task SetPasswordAsync()
    {
        if (PendingUser is null || !MustSetPassword)
        {
            return;
        }

        if (await UserManager.CheckPasswordAsync(PendingUser, Password.NewPassword))
        {
            ErrorMessage = "Новий пароль має відрізнятися від тимчасового.";
            return;
        }

        PendingUser.RequirePasswordChange(false);
        var token = await UserManager.GeneratePasswordResetTokenAsync(PendingUser);
        var result = await UserManager.ResetPasswordAsync(PendingUser, token, Password.NewPassword);
        if (!result.Succeeded)
        {
            PendingUser.RequirePasswordChange(true);
            ErrorMessage = string.Join(" ", result.Errors.Select(static e => e.Description));
            return;
        }

        await Audit.WriteAsync(AuditActions.PasswordChanged, PendingUser.Id, PendingUser.FullName, "Перший вхід: тимчасовий пароль замінено", CancellationToken.None);
        Redirect.RedirectTo("/account/setup-authenticator", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = RememberMe, ["firstLogin"] = true });
    }

    /// <summary>
    /// Creates the authenticator key (once) and renders the QR code.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>A task that completes when the key is ready.</returns>
    private async Task PrepareKeyAsync(User user)
    {
        var key = await UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await UserManager.ResetAuthenticatorKeyAsync(user);
            key = await UserManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKeyRaw = key!;
        SharedKey = AuthenticatorQrCode.FormatKey(key!);
        QrSvg = AuthenticatorQrCode.RenderSvg(AuthenticatorQrCode.BuildUri(user.Email!, key!));
    }

    /// <summary>
    /// Verifies the first code, enables two-factor authentication and signs the user in.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task EnrollAsync()
    {
        if (PendingUser is null || MustSetPassword)
        {
            return;
        }

        var code = Input.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var valid = await UserManager.VerifyTwoFactorTokenAsync(PendingUser, UserManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
        {
            await UserManager.AccessFailedAsync(PendingUser);
            if (await UserManager.IsLockedOutAsync(PendingUser))
            {
                await Audit.WriteAsync(AuditActions.LockedOut, PendingUser.Id, PendingUser.FullName, "Підключення автентифікатора", CancellationToken.None);
                Redirect.RedirectTo("/account/lockout");
                return;
            }

            ErrorMessage = "Код неправильний. Перевірте час на телефоні та спробуйте наступний код.";
            return;
        }

        if (!await SignInManager.CanSignInAsync(PendingUser))
        {
            Redirect.RedirectTo("/account/login");
            return;
        }

        await UserManager.SetTwoFactorEnabledAsync(PendingUser, true);
        RecoveryCodes = [.. await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(PendingUser, 10) ?? []];
        PendingUser.RecordSignIn(TimeProvider.GetUtcNow());
        await UserManager.UpdateAsync(PendingUser);

        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await SignInManager.SignInWithClaimsAsync(PendingUser, RememberMe, [new Claim(AppClaimTypes.AuthenticationMethod, "mfa")]);

        await Audit.WriteAsync(AuditActions.AuthenticatorEnrolled, PendingUser.Id, PendingUser.FullName, null, CancellationToken.None);
        await Audit.WriteAsync(AuditActions.SignIn, PendingUser.Id, PendingUser.FullName, "Пароль + новий автентифікатор", CancellationToken.None);
    }

    /// <summary>
    /// The enrollment form.
    /// </summary>
    private sealed class EnrollModel
    {
        /// <summary>
        /// Gets or sets the verification code.
        /// </summary>
        [Required(ErrorMessage = "Введіть код.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Код містить 6 цифр.")]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// The first password form.
    /// </summary>
    private sealed class PasswordModel
    {
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
