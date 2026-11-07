namespace ODC.Infrastructure.Identity.Activation;

/// <summary>
/// The answer to a code request: the cookie value that binds the activation to this browser and its state.
/// </summary>
/// <param name="Ticket">The encrypted cookie value.</param>
/// <param name="Status">The state.</param>
public sealed record ActivationStart(string Ticket, ActivationStatus Status);
