using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ODC.Application.Common.Results;
using ODC.Domain.Localization;
using ODC.Domain.Users;
using ODC.Infrastructure.Identity.Activation;
using ODC.Web.Api.Contracts;

namespace ODC.Web.Api;

/// <summary>
/// Activation of an account created by an administrator, step by step in one browser: the e-mail, the one-time code from
/// the e-mail, the first password. Then the usual sign-in steps follow (authenticator, photo). The activation is bound
/// to the browser by an encrypted <c>__Host-</c> cookie that scripts cannot read.
/// </summary>
/// <param name="activation">The activation service.</param>
/// <param name="signInManager">The sign-in manager.</param>
[Route("api/v1/auth/activation")]
[AllowAnonymous]
public sealed class AccountActivationController(AccountActivationService activation, SignInManager<User> signInManager) : ApiControllerBase
{
    /// <summary>
    /// The name of the rate limit policy of code requests.
    /// </summary>
    public const string RateLimitPolicy = "activation";

    /// <summary>
    /// The cookie that binds an activation to this browser.
    /// </summary>
    internal const string CookieName = "__Host-odc.activation";

    /// <summary>
    /// The error code of a browser without an activation.
    /// </summary>
    private const string NoActivationCode = "no_activation";

    /// <summary>
    /// Gets the activation of this browser (after a reload of the page).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state, or 404 when this browser has not started an activation.</returns>
    [HttpGet]
    public async Task<ActionResult<ActivationStatusResponse>> Get(CancellationToken cancellationToken)
    {
        if (CurrentFlow() is not { } flow)
        {
            return ProblemFor(NoActivation());
        }

        return ToResponse(await activation.GetStatusAsync(flow, cancellationToken));
    }

    /// <summary>
    /// Sends a one-time code to the e-mail when it belongs to an account waiting for activation. The answer is the same
    /// for every address, so the endpoint does not reveal which accounts exist.
    /// </summary>
    /// <param name="request">The e-mail.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state: enter the code.</returns>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<ActionResult<ActivationStatusResponse>> Start(ActivationStartRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await activation.StartAsync(request.Email, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemFor(result.Error);
        }

        Response.Cookies.Append(CookieName, result.Value.Ticket, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = AccountActivationService.TicketLifetime,
        });
        return ToResponse(result.Value.Status);
    }

    /// <summary>
    /// Checks the code from the e-mail.
    /// </summary>
    /// <param name="request">The code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account: what the next steps need.</returns>
    [HttpPost("code")]
    public async Task<ActionResult<ActivationVerifiedResponse>> Verify(ActivationCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (CurrentFlow() is not { } flow)
        {
            return ProblemFor(NoActivation());
        }

        var result = await activation.VerifyAsync(flow, request.Code, cancellationToken);
        return result.IsSuccess
            ? new ActivationVerifiedResponse(result.Value.FirstName, result.Value.HasAuthenticator, result.Value.HasPhoto)
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Sets the first password and starts the sign-in: the next step is the authenticator (enrolled now, or its code when
    /// one was enrolled before a password reset).
    /// </summary>
    /// <param name="request">The new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The next sign-in step.</returns>
    [HttpPost("password")]
    public async Task<ActionResult<LoginResponse>> Password(NewPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (CurrentFlow() is not { } flow)
        {
            return ProblemFor(NoActivation());
        }

        var result = await activation.CompleteAsync(flow, request.NewPassword, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemFor(result.Error);
        }

        DeleteCookie();
        var user = result.Value;

        // The password step of the usual sign-in: it opens the second factor step (mandatory for everyone).
        var signIn = await signInManager.PasswordSignInAsync(user, request.NewPassword, isPersistent: false, lockoutOnFailure: true);
        if (!signIn.RequiresTwoFactor && !signIn.Succeeded)
        {
            return ProblemFor(Error.Validation(Texts.T("Пароль збережено, але увійти зараз не вдалося. Увійдіть зі сторінки входу.")));
        }

        return new LoginResponse(user.TwoFactorEnabled ? LoginResponse.TwoFactor : LoginResponse.Setup);
    }

    /// <summary>
    /// Forgets the activation of this browser (to start over with another e-mail).
    /// </summary>
    /// <returns>204.</returns>
    [HttpDelete]
    public ActionResult Cancel()
    {
        DeleteCookie();
        return NoContent();
    }

    /// <summary>
    /// Converts a state to the response.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The response.</returns>
    private static ActivationStatusResponse ToResponse(ActivationStatus status) => new(status.Stage, status.Email, status.ResendAfterSeconds);

    /// <summary>
    /// The error of a browser without an activation.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error NoActivation()
        => new(NoActivationCode, Texts.T("Активацію не розпочато або час на неї минув. Почніть знову."), ErrorType.NotFound);

    /// <summary>
    /// Reads the activation of this browser.
    /// </summary>
    /// <returns>The activation, or <see langword="null"/>.</returns>
    private ActivationFlow? CurrentFlow() => activation.ReadTicket(Request.Cookies[CookieName]);

    /// <summary>
    /// Removes the activation cookie.
    /// </summary>
    private void DeleteCookie()
        => Response.Cookies.Delete(CookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" });
}
