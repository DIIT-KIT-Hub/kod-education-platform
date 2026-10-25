using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Account;

/// <summary>
/// Mandatory authenticator enrollment at the first sign-in (and after an administrator reset).
/// Uses the Identity authenticator token provider (TOTP, RFC 6238) and issues recovery codes.
/// </summary>
public partial class SetupAuthenticator
{
    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm]
    private EnrollModel Input { get; set; } = default!;

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
    /// Gets the address to continue to.
    /// </summary>
    private string ContinueHref => IdentityRedirectManager.SafeLocal(ReturnUrl);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Input ??= new EnrollModel();
        PendingUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (PendingUser is null)
        {
            Redirect.RedirectTo("/account/login");
            return;
        }

        if (PendingUser.TwoFactorEnabled)
        {
            Redirect.RedirectTo("/account/login-2fa", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = RememberMe });
            return;
        }

        var key = await UserManager.GetAuthenticatorKeyAsync(PendingUser);
        if (string.IsNullOrEmpty(key))
        {
            await UserManager.ResetAuthenticatorKeyAsync(PendingUser);
            key = await UserManager.GetAuthenticatorKeyAsync(PendingUser);
        }

        SharedKeyRaw = key!;
        SharedKey = AuthenticatorQrCode.FormatKey(key!);
        QrSvg = AuthenticatorQrCode.RenderSvg(AuthenticatorQrCode.BuildUri(PendingUser.Email!, key!));
    }

    /// <summary>
    /// Verifies the first code, enables two-factor authentication and signs the user in.
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task EnrollAsync()
    {
        if (PendingUser is null)
        {
            return;
        }

        var code = Input.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var valid = await UserManager.VerifyTwoFactorTokenAsync(PendingUser, UserManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
        {
            await UserManager.AccessFailedAsync(PendingUser);
            ErrorMessage = "Код неправильний. Перевірте час на телефоні та спробуйте наступний код.";
            return;
        }

        await UserManager.SetTwoFactorEnabledAsync(PendingUser, true);
        RecoveryCodes = [.. await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(PendingUser, 10) ?? []];
        PendingUser.RecordSignIn(TimeProvider.GetUtcNow());
        await UserManager.UpdateAsync(PendingUser);

        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await SignInManager.SignInWithClaimsAsync(PendingUser, RememberMe, [new Claim("amr", "mfa")]);

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
}
