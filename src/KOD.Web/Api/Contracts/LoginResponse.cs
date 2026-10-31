namespace KOD.Web.Api.Contracts;

/// <summary>
/// The step that follows a correct password: the authenticator code, or the enrollment of an authenticator
/// (after replacing a temporary password).
/// </summary>
/// <param name="Next"><c>two-factor</c> or <c>setup</c>.</param>
public sealed record LoginResponse(string Next)
{
    /// <summary>
    /// The user enters a code of the enrolled authenticator.
    /// </summary>
    public const string TwoFactor = "two-factor";

    /// <summary>
    /// The user replaces a temporary password (if needed) and enrolls an authenticator.
    /// </summary>
    public const string Setup = "setup";
}
