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
/// The first sign-in step: e-mail and password (ASP.NET Core Identity <see cref="SignInManager{TUser}"/>).
/// Every account is then routed to the second factor (authenticator code) or to the authenticator enrollment.
/// </summary>
public partial class Login
{
    /// <summary>
    /// Gets the answer for every failed attempt.
    /// </summary>
    private static string GenericError => Texts.T("Неправильна електронна пошта або пароль. Після кількох невдалих спроб вхід тимчасово блокується.");

    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm]
    private LoginModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the address to return to after signing in.
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
    /// Gets or sets the redirect manager.
    /// </summary>
    [Inject]
    private IdentityRedirectManager Redirect { get; set; } = default!;

    /// <summary>
    /// Gets or sets the error shown above the form.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() => Input ??= new LoginModel();

    /// <summary>
    /// Verifies the password and continues to the second factor. Unknown accounts, wrong passwords and locked-out
    /// accounts get the same answer, so the form does not reveal which e-mails exist (OWASP ASVS 2.2.1 / 3.2).
    /// </summary>
    /// <returns>A task that completes when the response is decided.</returns>
    private async Task SignInAsync()
    {
        var user = await UserManager.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            // Spend comparable time to avoid revealing which e-mails exist.
            _ = UserManager.PasswordHasher.HashPassword(new User("timing@kod.local", "-", "-", null), Input.Password);
            await Audit.WriteAsync(AuditActions.SignInFailed, null, Input.Email.Trim(), "Невідомий обліковий запис", CancellationToken.None);
            ErrorMessage = GenericError;
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.RequiresTwoFactor || result.Succeeded)
        {
            Redirect.RedirectTo("/account/login-2fa", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = Input.RememberMe });
        }
        else if (result.IsLockedOut)
        {
            // Identity reports a lockout before checking the password: answering differently would reveal the account
            // and let a locked-out attacker keep testing passwords. Hash anyway so the timing matches.
            _ = UserManager.PasswordHasher.HashPassword(user, Input.Password);
            await Audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, "Спроба входу під час блокування", CancellationToken.None);
            ErrorMessage = GenericError;
        }
        else if (result.IsNotAllowed)
        {
            // A blocked account: say so only to someone who knows the password; wrong guesses count toward the lockout.
            if (await UserManager.CheckPasswordAsync(user, Input.Password))
            {
                await Audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Обліковий запис заблоковано", CancellationToken.None);
                ErrorMessage = Texts.T("Обліковий запис заблоковано. Зверніться до адміністратора.");
            }
            else
            {
                await UserManager.AccessFailedAsync(user);
                await Audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль (заблокований обліковий запис)", CancellationToken.None);
                ErrorMessage = GenericError;
            }
        }
        else
        {
            await Audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль", CancellationToken.None);
            ErrorMessage = GenericError;
        }
    }

    /// <summary>
    /// The sign-in form.
    /// </summary>
    private sealed class LoginModel
    {
        /// <summary>
        /// Gets or sets the e-mail.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Некоректна адреса електронної пошти.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть пароль.")]
        [MaxLength(128, ErrorMessage = "Пароль не може бути довшим за 128 символів.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the session survives closing the browser.
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
