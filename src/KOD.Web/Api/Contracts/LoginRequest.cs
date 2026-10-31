namespace KOD.Web.Api.Contracts;

/// <summary>
/// The password step of the sign-in.
/// </summary>
/// <param name="Email">The e-mail address.</param>
/// <param name="Password">The password.</param>
/// <param name="RememberMe">Whether the sign-in survives closing the browser.</param>
public sealed record LoginRequest(string Email, string Password, bool RememberMe);
