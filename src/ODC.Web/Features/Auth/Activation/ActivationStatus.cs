namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The state of an activation.
/// </summary>
/// <param name="Stage">The stage.</param>
/// <param name="Email">The address typed by the user.</param>
/// <param name="ResendAfterSeconds">Seconds until another code can be requested (0 — now).</param>
public sealed record ActivationStatus(ActivationStage Stage, string Email, int ResendAfterSeconds);
