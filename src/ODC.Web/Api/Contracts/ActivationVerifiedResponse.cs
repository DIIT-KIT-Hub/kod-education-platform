namespace ODC.Web.Api.Contracts;

/// <summary>
/// The account whose activation code was entered correctly.
/// </summary>
/// <param name="FirstName">The first name, for the greeting.</param>
/// <param name="HasAuthenticator">Whether an authenticator is enrolled (then its code is asked for instead of enrolling one).</param>
/// <param name="HasPhoto">Whether a profile photo is set (then the photo step is skipped).</param>
public sealed record ActivationVerifiedResponse(string FirstName, bool HasAuthenticator, bool HasPhoto);
