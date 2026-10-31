namespace KOD.Web.Api.Contracts;

/// <summary>
/// A one-time code: an authenticator (TOTP) code or a recovery code.
/// </summary>
/// <param name="Code">The code.</param>
/// <param name="RememberMe">Whether the sign-in survives closing the browser.</param>
public sealed record CodeRequest(string Code, bool RememberMe);
