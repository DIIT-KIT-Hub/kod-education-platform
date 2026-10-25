using System.ComponentModel.DataAnnotations;
using KOD.Application.Abstractions;
using KOD.Domain.Audit;
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
    /// Verifies the password and continues to the second factor.
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
            ErrorMessage = "Неправильна електронна пошта або пароль.";
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.RequiresTwoFactor || result.Succeeded)
        {
            Redirect.RedirectTo("/account/login-2fa", new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = Input.RememberMe });
        }
        else if (result.IsLockedOut)
        {
            await Audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, null, CancellationToken.None);
            Redirect.RedirectTo("/account/lockout");
        }
        else if (result.IsNotAllowed)
        {
            ErrorMessage = "Обліковий запис заблоковано. Зверніться до адміністратора.";
        }
        else
        {
            await Audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль", CancellationToken.None);
            ErrorMessage = "Неправильна електронна пошта або пароль.";
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
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the session survives closing the browser.
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
