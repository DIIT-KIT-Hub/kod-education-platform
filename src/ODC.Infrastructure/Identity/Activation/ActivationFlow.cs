namespace ODC.Infrastructure.Identity.Activation;

/// <summary>
/// The activation a browser follows, read from its encrypted cookie.
/// </summary>
/// <param name="ChallengeId">The code request.</param>
/// <param name="Email">The address typed by the user (shown back to them).</param>
public sealed record ActivationFlow(Guid ChallengeId, string Email);
