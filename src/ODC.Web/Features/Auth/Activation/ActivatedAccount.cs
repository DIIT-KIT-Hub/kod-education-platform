namespace ODC.Web.Features.Auth.Activation;

/// <summary>
/// The account whose code was entered correctly: what the activation page needs for the next steps.
/// </summary>
/// <param name="FirstName">The first name, for the greeting.</param>
/// <param name="HasAuthenticator">Whether an authenticator is enrolled (after a password reset it is asked for, not enrolled).</param>
/// <param name="HasPhoto">Whether a profile photo is set.</param>
public sealed record ActivatedAccount(string FirstName, bool HasAuthenticator, bool HasPhoto);
