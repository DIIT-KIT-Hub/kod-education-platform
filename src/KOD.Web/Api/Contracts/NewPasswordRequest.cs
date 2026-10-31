namespace KOD.Web.Api.Contracts;

/// <summary>
/// The user's own password replacing a temporary one at the first sign-in.
/// </summary>
/// <param name="NewPassword">The new password.</param>
public sealed record NewPasswordRequest(string NewPassword);
