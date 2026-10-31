using System.Security.Claims;
using KOD.Application.Abstractions;
using KOD.Application.Common.Results;
using KOD.Application.Features.Profile;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity;
using KOD.Web.Api.Contracts;
using KOD.Web.Identity;
using KOD.Web.Localization;
using KOD.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Sign-in, second factor, authenticator enrollment, password and language for the single-page application. The tokens
/// stay in <c>HttpOnly</c> cookies set by the JWT handler; the responses carry no secrets except the authenticator key
/// and the recovery codes, shown once to the user who owns them.
/// </summary>
/// <param name="signInManager">The sign-in manager.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="profile">The profile service.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="antiforgery">Issues antiforgery tokens.</param>
/// <param name="timeProvider">The clock.</param>
[Route("api/v1/auth")]
public sealed class AuthController(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IProfileService profile,
    IAuditLogger audit,
    IAntiforgery antiforgery,
    TimeProvider timeProvider) : ApiControllerBase
{
    /// <summary>
    /// The error code of a temporarily locked account.
    /// </summary>
    private const string LockedOutCode = "locked_out";

    /// <summary>
    /// The error code of a sign-in that has no pending password step (it expired or never happened).
    /// </summary>
    private const string NoPendingSignInCode = "no_pending_sign_in";

    /// <summary>
    /// Gets the answer to every failed password attempt.
    /// </summary>
    private static string GenericError => Texts.T("Неправильна електронна пошта або пароль. Після кількох невдалих спроб вхід тимчасово блокується.");

    /// <summary>
    /// Gets the start context of the application: language, antiforgery token and the signed-in user.
    /// </summary>
    /// <returns>The context.</returns>
    [HttpGet("context")]
    [AllowAnonymous]
    public ActionResult<AuthContextResponse> Context()
    {
        var principal = HttpContext.User;
        var user = principal.UserId is { } id
            ? new CurrentUserResponse(
                id,
                principal.FindFirstValue(AppClaimTypes.Email) ?? string.Empty,
                principal.FullName,
                principal.ShortName,
                principal.PhotoId,
                principal.MustChangePassword,
                [.. principal.FindAll(AppClaimTypes.Permission).Select(static c => c.Value)])
            : null;

        return new AuthContextResponse(Languages.Current, antiforgery.GetAndStoreTokens(HttpContext).RequestToken ?? string.Empty, user);
    }

    /// <summary>
    /// Verifies the e-mail and password. Unknown accounts, wrong passwords and locked-out accounts get the same answer,
    /// so the endpoint does not reveal which e-mails exist (OWASP ASVS 2.2.1 / 3.2).
    /// </summary>
    /// <param name="request">The credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The next step.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = request.Email?.Trim() ?? string.Empty;
        var user = email.Length == 0 ? null : await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Spend comparable time to avoid revealing which e-mails exist.
            _ = userManager.PasswordHasher.HashPassword(new User("timing@kod.local", "-", "-", null), request.Password ?? string.Empty);
            await audit.WriteAsync(AuditActions.SignInFailed, null, email, "Невідомий обліковий запис", cancellationToken);
            return ProblemFor(Error.Validation(GenericError));
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password ?? string.Empty, request.RememberMe, lockoutOnFailure: true);
        if (result.RequiresTwoFactor || result.Succeeded)
        {
            return new LoginResponse(user.TwoFactorEnabled ? LoginResponse.TwoFactor : LoginResponse.Setup);
        }

        if (result.IsLockedOut)
        {
            // Identity reports a lockout before checking the password: answering differently would reveal the account
            // and let a locked-out attacker keep testing passwords. Hash anyway so the timing matches.
            _ = userManager.PasswordHasher.HashPassword(user, request.Password ?? string.Empty);
            await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, "Спроба входу під час блокування", cancellationToken);
            return ProblemFor(Error.Validation(GenericError));
        }

        if (result.IsNotAllowed)
        {
            // A blocked account: say so only to someone who knows the password; wrong guesses count toward the lockout.
            if (await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
            {
                await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Обліковий запис заблоковано", cancellationToken);
                return ProblemFor(Error.Validation(Texts.T("Обліковий запис заблоковано. Зверніться до адміністратора.")));
            }

            await userManager.AccessFailedAsync(user);
            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль (заблокований обліковий запис)", cancellationToken);
            return ProblemFor(Error.Validation(GenericError));
        }

        await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний пароль", cancellationToken);
        return ProblemFor(Error.Validation(GenericError));
    }

    /// <summary>
    /// Describes the sign-in waiting for the second factor.
    /// </summary>
    /// <returns>The pending sign-in, or 401 when there is none.</returns>
    [HttpGet("login/pending")]
    [AllowAnonymous]
    public async Task<ActionResult<PendingSignInResponse>> Pending()
    {
        var user = await PendingUserAsync();
        return user is null
            ? ProblemFor(NoPendingSignIn())
            : new PendingSignInResponse(user.MustChangePassword, user.TwoFactorEnabled);
    }

    /// <summary>
    /// Completes the sign-in with an authenticator code.
    /// </summary>
    /// <param name="request">The code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 when signed in.</returns>
    [HttpPost("login/two-factor")]
    [AllowAnonymous]
    public async Task<ActionResult> TwoFactor(CodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await PendingUserAsync() is not { TwoFactorEnabled: true } user)
        {
            return ProblemFor(NoPendingSignIn());
        }

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(NormalizeCode(request.Code), request.RememberMe, rememberClient: false);
        if (result.Succeeded)
        {
            await RecordSignInAsync(user, "Пароль + автентифікатор", cancellationToken);
            return NoContent();
        }

        if (result.IsLockedOut)
        {
            await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, null, cancellationToken);
            return ProblemFor(LockedOut());
        }

        await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний код автентифікатора", cancellationToken);
        return ProblemFor(Error.Validation(Texts.T("Код неправильний або вже недійсний. Спробуйте наступний код.")));
    }

    /// <summary>
    /// Completes the sign-in with a one-time recovery code instead of the authenticator.
    /// </summary>
    /// <param name="request">The recovery code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 when signed in.</returns>
    [HttpPost("login/recovery")]
    [AllowAnonymous]
    public async Task<ActionResult> Recovery(CodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await PendingUserAsync() is not { TwoFactorEnabled: true } user)
        {
            return ProblemFor(NoPendingSignIn());
        }

        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync((request.Code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal));
        if (result.Succeeded)
        {
            await RecordSignInAsync(user, "Пароль + резервний код", cancellationToken);
            return NoContent();
        }

        if (result.IsLockedOut)
        {
            return ProblemFor(LockedOut());
        }

        await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний резервний код", cancellationToken);
        return ProblemFor(Error.Validation(Texts.T("Резервний код недійсний.")));
    }

    /// <summary>
    /// Replaces the temporary password at the first sign-in, before an authenticator is enrolled. The user proved
    /// knowledge of it minutes ago (the pending sign-in lives 10 minutes), so the reset-token path of Identity is used.
    /// </summary>
    /// <param name="request">The new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 when saved.</returns>
    [HttpPost("login/password")]
    [AllowAnonymous]
    public async Task<ActionResult> FirstPassword(NewPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await PendingUserAsync() is not { MustChangePassword: true, TwoFactorEnabled: false } user)
        {
            return ProblemFor(NoPendingSignIn());
        }

        var password = request.NewPassword ?? string.Empty;
        if (await userManager.CheckPasswordAsync(user, password))
        {
            return ProblemFor(Error.Validation(Texts.T("Новий пароль має відрізнятися від тимчасового.")));
        }

        user.RequirePasswordChange(false);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            user.RequirePasswordChange(true);
            return ProblemFor(Error.Validation(string.Join(" ", result.Errors.Select(static e => e.Description))));
        }

        await audit.WriteAsync(AuditActions.PasswordChanged, user.Id, user.FullName, "Перший вхід: тимчасовий пароль замінено", cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Creates the authenticator key of the pending user (once) with its QR code.
    /// </summary>
    /// <returns>The key.</returns>
    [HttpGet("login/authenticator")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatorSetupResponse>> AuthenticatorKey()
    {
        if (await PendingUserAsync() is not { MustChangePassword: false, TwoFactorEnabled: false } user)
        {
            return ProblemFor(NoPendingSignIn());
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        return new AuthenticatorSetupResponse(key!, AuthenticatorQrCode.FormatKey(key!), AuthenticatorQrCode.RenderSvg(AuthenticatorQrCode.BuildUri(user.Email!, key!)));
    }

    /// <summary>
    /// Verifies the first authenticator code, enables two-factor authentication, issues recovery codes and signs in.
    /// </summary>
    /// <param name="request">The code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The recovery codes, shown once.</returns>
    [HttpPost("login/authenticator")]
    [AllowAnonymous]
    public async Task<ActionResult<RecoveryCodesResponse>> EnrollAuthenticator(CodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await PendingUserAsync() is not { MustChangePassword: false, TwoFactorEnabled: false } user)
        {
            return ProblemFor(NoPendingSignIn());
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return ProblemFor(LockedOut());
        }

        if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, NormalizeCode(request.Code)))
        {
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                await audit.WriteAsync(AuditActions.LockedOut, user.Id, user.FullName, "Підключення автентифікатора", cancellationToken);
                return ProblemFor(LockedOut());
            }

            return ProblemFor(Error.Validation(Texts.T("Код неправильний. Перевірте час на телефоні та спробуйте наступний код.")));
        }

        if (!await signInManager.CanSignInAsync(user))
        {
            return ProblemFor(NoPendingSignIn());
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? [];
        user.RecordSignIn(timeProvider.GetUtcNow());
        await userManager.UpdateAsync(user);

        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await signInManager.SignInWithClaimsAsync(user, request.RememberMe, [new Claim(AppClaimTypes.AuthenticationMethod, "mfa")]);

        await audit.WriteAsync(AuditActions.AuthenticatorEnrolled, user.Id, user.FullName, null, cancellationToken);
        await audit.WriteAsync(AuditActions.SignIn, user.Id, user.FullName, "Пароль + новий автентифікатор", cancellationToken);
        return new RecoveryCodesResponse([.. codes]);
    }

    /// <summary>
    /// Signs out: revokes the refresh token family and removes the token cookies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204.</returns>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var user = HttpContext.User;
        if (user.UserId is not null)
        {
            await audit.WriteAsync(AuditActions.SignOut, user.UserId, user.FullName, null, cancellationToken);
        }

        await signInManager.SignOutAsync();
        return NoContent();
    }

    /// <summary>
    /// Changes the password of the signed-in user (also the forced replacement of a temporary password) and re-issues
    /// the tokens of this browser; the other sign-ins end.
    /// </summary>
    /// <param name="request">The passwords.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 when changed.</returns>
    [HttpPost("password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await profile.ChangePasswordAsync(request.CurrentPassword ?? string.Empty, request.NewPassword ?? string.Empty, cancellationToken);
        if (result.IsSuccess && await userManager.GetUserAsync(User) is { } user)
        {
            await signInManager.RefreshSignInAsync(user);
        }

        return FromResult(result);
    }

    /// <summary>
    /// Changes the interface language: remembered in this browser and, for a signed-in user, saved in the profile with
    /// the tokens re-issued so the new language applies at once.
    /// </summary>
    /// <param name="request">The language.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204.</returns>
    [HttpPut("language")]
    [AllowAnonymous]
    public async Task<ActionResult> Language(LanguageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (Languages.Normalize(request.Language) is not { } code)
        {
            return ProblemFor(Error.Validation(Texts.T("Непідтримувана мова інтерфейсу.")));
        }

        if (HttpContext.User.UserId is not null)
        {
            var saved = await profile.SetLanguageAsync(code, cancellationToken);
            if (!saved.IsSuccess)
            {
                return ProblemFor(saved.Error);
            }

            if (await userManager.GetUserAsync(User) is { } user)
            {
                await signInManager.RefreshSignInAsync(user);
            }
        }

        LanguageCultureProvider.Remember(Response, code);
        return NoContent();
    }

    /// <summary>
    /// Normalizes an authenticator code typed with spaces or a dash.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The digits.</returns>
    private static string NormalizeCode(string? code)
        => (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// The error of a temporarily locked account.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error LockedOut()
        => new(LockedOutCode, Texts.T("Вхід до облікового запису тимчасово заблоковано на 15 хвилин через кілька невдалих спроб. Спробуйте пізніше або зверніться до адміністратора."), ErrorType.Forbidden);

    /// <summary>
    /// The error of a missing password step.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error NoPendingSignIn()
        => new(NoPendingSignInCode, Texts.T("Час на підтвердження входу минув. Увійдіть знову."), ErrorType.Unauthorized);

    /// <summary>
    /// Gets the active user who passed the password step.
    /// </summary>
    /// <returns>The user, or <see langword="null"/>.</returns>
    private async Task<User?> PendingUserAsync()
        => await signInManager.GetTwoFactorAuthenticationUserAsync() is { IsActive: true } user ? user : null;

    /// <summary>
    /// Records a completed sign-in.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="method">How the user signed in (audit details).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when recorded.</returns>
    private async Task RecordSignInAsync(User user, string method, CancellationToken cancellationToken)
    {
        user.RecordSignIn(timeProvider.GetUtcNow());
        await userManager.UpdateAsync(user);
        await audit.WriteAsync(AuditActions.SignIn, user.Id, user.FullName, method, cancellationToken);
    }
}
