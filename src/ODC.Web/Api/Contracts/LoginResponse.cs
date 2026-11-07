namespace ODC.Web.Api.Contracts;

/// <summary>
/// The step that follows a correct password: the authenticator code, or the enrollment of an authenticator.
/// </summary>
/// <param name="Next"><c>two-factor</c> or <c>setup</c>.</param>
public sealed record LoginResponse(string Next)
{
    /// <summary>
    /// The user enters a code of the enrolled authenticator.
    /// </summary>
    public const string TwoFactor = "two-factor";

    /// <summary>
    /// The user enrolls an authenticator (the first sign-in, or after an administrator reset it).
    /// </summary>
    public const string Setup = "setup";
}
