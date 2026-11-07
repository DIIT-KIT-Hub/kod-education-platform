using ODC.Infrastructure.Identity.Activation;

namespace ODC.Web.Api.Contracts;

/// <summary>
/// The activation this browser follows.
/// </summary>
/// <param name="Stage">The stage: enter the code, choose the password, or request a new code.</param>
/// <param name="Email">The e-mail typed at the first step.</param>
/// <param name="ResendAfterSeconds">Seconds until another code can be requested (0 — now).</param>
public sealed record ActivationStatusResponse(ActivationStage Stage, string Email, int ResendAfterSeconds);
