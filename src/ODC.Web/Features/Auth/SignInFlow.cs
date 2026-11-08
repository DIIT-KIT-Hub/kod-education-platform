using Microsoft.AspNetCore.Identity;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// The steps of a sign-in the slices share: the user who passed the password step (the short-lived <c>2fa+jwt</c>
/// cookie), the stable error codes the client relies on and the record of a completed sign-in.
/// </summary>
/// <param name="signInManager">The sign-in manager.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class SignInFlow(SignInManager<User> signInManager, UserManager<User> userManager, IAuditLogger audit, TimeProvider timeProvider)
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
    public static string GenericError => Texts.T("Неправильна електронна пошта або пароль. Після кількох невдалих спроб вхід тимчасово блокується.");

    /// <summary>
    /// Normalizes an authenticator code typed with spaces or a dash.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The digits.</returns>
    public static string NormalizeCode(string? code)
        => (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// The error of a temporarily locked account.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error LockedOut()
        => new(LockedOutCode, Texts.T("Вхід до облікового запису тимчасово заблоковано на 15 хвилин через кілька невдалих спроб. Спробуйте пізніше або зверніться до адміністратора."), ErrorType.Forbidden);

    /// <summary>
    /// The error of a missing password step.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NoPendingSignIn()
        => new(NoPendingSignInCode, Texts.T("Час на підтвердження входу минув. Увійдіть знову."), ErrorType.Unauthorized);

    /// <summary>
    /// Gets the active user who passed the password step.
    /// </summary>
    /// <returns>The user, or <see langword="null"/>.</returns>
    public async Task<User?> PendingUserAsync()
        => await signInManager.GetTwoFactorAuthenticationUserAsync() is { IsActive: true } user ? user : null;

    /// <summary>
    /// Records a completed sign-in.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="method">How the user signed in (audit details).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when recorded.</returns>
    public async Task RecordSignInAsync(User user, string method, CancellationToken cancellationToken)
    {
        user.RecordSignIn(timeProvider.GetUtcNow());
        await userManager.UpdateAsync(user);
        await audit.WriteAsync(AuditActions.SignIn, user.Id, user.FullName, method, cancellationToken);
    }
}
