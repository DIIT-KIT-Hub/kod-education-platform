namespace KOD.Web.Api.Contracts;

/// <summary>
/// A password change of the signed-in user.
/// </summary>
/// <param name="CurrentPassword">The current password.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
