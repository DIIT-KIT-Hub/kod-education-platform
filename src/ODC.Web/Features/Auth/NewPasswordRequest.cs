namespace ODC.Web.Features.Auth;

/// <summary>
/// The password the owner chooses when activating the account.
/// </summary>
/// <param name="NewPassword">The new password.</param>
public sealed record NewPasswordRequest(string NewPassword);
